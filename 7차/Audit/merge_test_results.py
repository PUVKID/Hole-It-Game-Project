#!/usr/bin/env python3
"""Publish a case-level NUnit aggregate only after all selected results pass.

Run after the final supplementary run has completed, for example:
  python merge_test_results.py --final Tests-remaining.xml Tests-campaign.xml \
      --diagnostic TraceStage3.xml

The baseline is Tests-second.xml. Positional XML files replace matching cases
and add new cases in chronological order. Earlier failures remain in provenance.
No input XML or Unity project file is changed.
"""

from __future__ import annotations

import argparse
import copy
from collections import Counter, defaultdict
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import sys
import tempfile
from xml.etree import ElementTree as ET


AUDIT = Path(__file__).resolve().parent
EXPECTED_BASE_COUNT = 75
REQUIRED_UI_CASES = {
    "ReferenceTouchTests.FinderMarkersStayVisibleInOverlayAndCameraCanvasAndExpireCleanly",
    "ReferenceTouchTests.OriginalBombMessageRetainsItsLightSourcePanelAndDarkSourceText",
}
OUTPUT_NAMES = ("Tests.xml", "Tests.log", "TestProvenance.json")


class InvalidResults(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise InvalidResults(message)


def resolve_input(filename: str) -> Path:
    path = Path(filename)
    return (path if path.is_absolute() else AUDIT / path).resolve()


def timestamp(value: str | None, context: str) -> datetime:
    require(bool(value), f"{context}: missing completion timestamp")
    try:
        result = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except (TypeError, ValueError) as error:
        raise InvalidResults(f"{context}: invalid timestamp {value!r}") from error
    require(result.tzinfo is not None, f"{context}: timezone must be explicit")
    return result.astimezone(timezone.utc)


def number(value: str | None, context: str) -> float:
    try:
        result = float(value)
    except (TypeError, ValueError) as error:
        raise InvalidResults(f"{context}: invalid numeric value {value!r}") from error
    require(math.isfinite(result) and result >= 0, f"{context}: negative/nonfinite value")
    return result


def integer(value: str | None, context: str) -> int:
    result = number(value, context)
    require(result.is_integer(), f"{context}: expected an integer")
    return int(result)


def load_run(filename: str) -> dict:
    path = resolve_input(filename)
    require(path.is_file(), f"Missing completed results file: {path}")
    before = path.stat()
    raw = path.read_bytes()
    after = path.stat()
    require((before.st_size, before.st_mtime_ns) == (after.st_size, after.st_mtime_ns),
            f"{path.name}: file changed while being read; wait for the run to finish")
    try:
        root = ET.fromstring(raw)
    except ET.ParseError as error:
        raise InvalidResults(f"{path.name}: incomplete or invalid XML: {error}") from error
    require(root.tag == "test-run", f"{path.name}: expected NUnit 3 test-run root")
    start = timestamp(root.get("start-time"), path.name + " start-time")
    end = timestamp(root.get("end-time"), path.name + " end-time")
    require(end >= start, f"{path.name}: run ends before it starts")
    duration = number(root.get("duration"), path.name + " duration")
    result = root.get("result", "")
    require(result.startswith(("Passed", "Failed", "Skipped", "Inconclusive", "Warning")),
            f"{path.name}: run has no completed result: {result!r}")
    cases = {}
    for case in root.iter("test-case"):
        fullname = case.get("fullname")
        require(bool(fullname), f"{path.name}: case without fullname")
        require(fullname not in cases, f"{path.name}: duplicate case fullname {fullname}")
        require(case.get("result") in {"Passed", "Failed", "Skipped", "Inconclusive", "Warning"},
                f"{path.name}: unfinished/unknown case result for {fullname}")
        # Executed cases must have complete timestamps. Skips will be rejected
        # if selected, but can remain in an older run's history.
        if case.get("result") not in {"Skipped"}:
            case_start = timestamp(case.get("start-time"), fullname + " start-time")
            case_end = timestamp(case.get("end-time"), fullname + " end-time")
            require(case_end >= case_start, f"{path.name}: negative case interval: {fullname}")
        number(case.get("duration", "0"), fullname + " duration")
        integer(case.get("asserts", "0"), fullname + " asserts")
        require(not (case.get("result") == "Passed" and case.find("failure") is not None),
                f"{path.name}: passed case contains failure data: {fullname}")
        cases[fullname] = case
    require(bool(cases), f"{path.name}: no executed/listed test cases")
    counts = Counter(case.get("result") for case in cases.values())
    require(integer(root.get("total"), path.name + " total") == len(cases),
            f"{path.name}: root total does not match its case list")
    require(integer(root.get("testcasecount"), path.name + " testcasecount") == len(cases),
            f"{path.name}: expected-case count does not match its case list")
    for attribute, case_result in (("passed", "Passed"), ("failed", "Failed"),
                                   ("skipped", "Skipped"), ("inconclusive", "Inconclusive")):
        require(integer(root.get(attribute, "0"), path.name + " " + attribute) == counts[case_result],
                f"{path.name}: root {attribute} count disagrees with case results")
    require(not (result.startswith("Failed") and counts["Failed"] == 0),
            f"{path.name}: suite-level failure has no failed case to replace")
    require(not (result == "Passed" and counts["Passed"] != len(cases)),
            f"{path.name}: passed run contains a non-passing case")
    return {"path": path, "root": root, "cases": cases, "counts": dict(counts),
            "start": start, "end": end, "duration": duration,
            "sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw)}


def source_record(run: dict) -> dict:
    return {"filename": run["path"].name, "path": str(run["path"]),
            "sha256": run["sha256"], "bytes": run["bytes"],
            "result": run["root"].get("result"), "case_count": len(run["cases"]),
            "counts": run["counts"], "start_time": run["root"].get("start-time"),
            "end_time": run["root"].get("end-time"), "duration_seconds": run["duration"],
            "operator_note": run.get("note", "")}


def case_record(case: ET.Element, run: dict) -> dict:
    entry = {"source_run": run["path"].name, "source_path": str(run["path"]),
             "source_sha256": run["sha256"], "original_case_id": case.get("id"),
             "result": case.get("result"), "start_time": case.get("start-time"),
             "end_time": case.get("end-time"),
             "duration_seconds": number(case.get("duration", "0"), "case duration")}
    message = case.findtext("failure/message")
    if message:
        entry["failure_message"] = message.strip()
    return entry


def properties(element: ET.Element, values: dict[str, str]) -> None:
    container = element.find("properties")
    if container is None:
        container = ET.Element("properties")
        element.insert(0, container)
    for name, value in values.items():
        ET.SubElement(container, "property", {"name": name, "value": value})


def stats(cases: list[ET.Element]) -> dict[str, str]:
    starts = [timestamp(case.get("start-time"), "selected case start") for case in cases]
    ends = [timestamp(case.get("end-time"), "selected case end") for case in cases]
    return {"testcasecount": str(len(cases)), "total": str(len(cases)), "result": "Passed",
            "passed": str(len(cases)), "failed": "0", "inconclusive": "0", "skipped": "0",
            "asserts": str(sum(integer(c.get("asserts", "0"), "asserts") for c in cases)),
            "start-time": min(starts).strftime("%Y-%m-%d %H:%M:%SZ"),
            "end-time": max(ends).strftime("%Y-%m-%d %H:%M:%SZ"),
            "duration": format(sum(number(c.get("duration", "0"), "duration") for c in cases), ".7f")}


def verify_unchanged(runs: list[dict]) -> None:
    for run in runs:
        require(hashlib.sha256(run["path"].read_bytes()).hexdigest() == run["sha256"],
                f"{run['path'].name}: changed during aggregation; no results published")


def publish(files: dict[str, bytes]) -> None:
    # Stage every complete output before replacing any previous report. The input
    # run artifacts remain untouched, including earlier failed runs.
    staged = []
    try:
        for name, data in files.items():
            with tempfile.NamedTemporaryFile(prefix=name + ".", suffix=".tmp", dir=AUDIT, delete=False) as handle:
                handle.write(data)
                handle.flush()
                os.fsync(handle.fileno())
                staged.append((Path(handle.name), AUDIT / name))
        for temporary, destination in staged:
            os.replace(temporary, destination)
    finally:
        for temporary, _ in staged:
            if temporary.exists():
                temporary.unlink()


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("overlays", nargs="*", help="Completed replacement/additional XML runs, oldest first")
    parser.add_argument("--base", default="Tests-second.xml")
    parser.add_argument("--diagnostic", action="append", default=[], help="Describe separately; never count as regression coverage")
    parser.add_argument("--source-note", action="append", default=[], metavar="FILE=NOTE",
                        help="Record a verified harness change or run limitation in provenance and the summary")
    parser.add_argument("--final", action="store_true", help="Explicitly confirm all supplied run files are final")
    args = parser.parse_args(argv)
    require(args.final, "No outputs written. Use --final only after every supplied run has completed.")
    runs = [load_run(args.base)] + [load_run(name) for name in (args.overlays or ["Tests-remaining.xml"])]
    diagnostics = [load_run(name) for name in args.diagnostic]
    for specification in args.source_note:
        filename, separator, note = specification.partition("=")
        require(bool(separator and filename and note.strip()), "Source note must use FILE=NOTE")
        matching = [run for run in runs + diagnostics if run["path"] == resolve_input(filename)]
        require(len(matching) == 1, f"Source note does not identify exactly one supplied run: {filename}")
        require("note" not in matching[0], f"Duplicate source note: {filename}")
        matching[0]["note"] = note.strip()
    paths = [run["path"] for run in runs + diagnostics]
    require(len(paths) == len(set(paths)), "The same source file was supplied more than once")
    require(not ({AUDIT / name for name in OUTPUT_NAMES} & set(paths)), "An output report cannot also be an input run")
    require(len(runs[0]["cases"]) == EXPECTED_BASE_COUNT,
            f"Baseline must contain the verified {EXPECTED_BASE_COUNT} cases; found {len(runs[0]['cases'])}")
    for earlier, later in zip(runs, runs[1:]):
        require(later["start"] >= earlier["end"],
                f"Runs must be supplied in chronological, non-overlapping order: {later['path'].name}")
    selected = {}
    history = defaultdict(list)
    merge_steps = []
    for run in runs:
        replacements = additions = 0
        for fullname, case in run["cases"].items():
            require("StageProbeTests" not in case.get("classname", "") and run["path"].name != "TraceStage3.xml",
                    f"{fullname}: stage probes are diagnostic only; supply that XML with --diagnostic")
            replacements += fullname in selected
            additions += fullname not in selected
            history[fullname].append(case_record(case, run))
            selected[fullname] = (case, run)
        merge_steps.append({"source_run": run["path"].name, "replaced_cases": replacements,
                            "new_cases": additions, "unique_cases_after_merge": len(selected)})
    missing_base = sorted(set(runs[0]["cases"]) - set(selected))
    require(not missing_base, "Missing baseline cases: " + ", ".join(missing_base))
    require(REQUIRED_UI_CASES <= set(selected),
            "Missing added UI regressions: " + ", ".join(sorted(REQUIRED_UI_CASES - set(selected))))
    not_passed = [(name, case.get("result"), run["path"].name)
                  for name, (case, run) in selected.items() if case.get("result") != "Passed"]
    require(not not_passed, "No outputs written; selected cases still do not pass:\n" +
            "\n".join(f"  {name}: {result} ({source})" for name, result, source in not_passed))

    selected_cases = [case for case, _ in selected.values()]
    aggregate_stats = stats(selected_cases)
    root = ET.Element("test-run", {"id": "0", **aggregate_stats})
    command = ET.SubElement(root, "command-line")
    command.text = "merge_test_results.py: case-level aggregate of separately completed NUnit runs"
    assembly = ET.SubElement(root, "test-suite", {
        "id": "1", "type": "Assembly", "name": "Aggregate from separate runs",
        "fullname": "HoleIt.Reference.Tests.AggregateFromSeparateRuns", "runstate": "Runnable", **aggregate_stats})
    properties(assembly, {
        "AggregationMode": "Latest completed case result replaces earlier result by fullname",
        "SourceRuns": "; ".join(run["path"].name for run in runs),
        "DurationMeaning": "Sum of selected case durations across separate runs",
        "CoverageBaseline": f"All {EXPECTED_BASE_COUNT} cases from {runs[0]['path'].name}",
        "DiagnosticExclusion": "Stage probes are described separately and excluded from pass counts",
    })
    fixtures = defaultdict(list)
    for fullname, (case, run) in selected.items():
        fixtures[case.get("classname") or fullname.rsplit(".", 1)[0]].append((fullname, case, run))
    next_id = 2
    provenance_cases = {}
    for classname in sorted(fixtures):
        entries = sorted(fixtures[classname], key=lambda item: item[0])
        fixture = ET.SubElement(assembly, "test-suite", {
            "id": str(next_id), "type": "TestFixture", "name": classname, "fullname": classname,
            "classname": classname, "runstate": "Runnable", **stats([case for _, case, _ in entries])})
        next_id += 1
        for fullname, case, run in entries:
            copied = copy.deepcopy(case)
            copied.set("id", str(next_id))
            next_id += 1
            properties(copied, {"AggregateSourceRun": run["path"].name,
                                "AggregateSourceSHA256": run["sha256"],
                                "OriginalCaseId": case.get("id", "")})
            fixture.append(copied)
            provenance_cases[fullname] = {
                "aggregate_case_id": copied.get("id"), "selected": case_record(case, run),
                "history": history[fullname], "baseline_case": fullname in runs[0]["cases"],
            }
    ET.indent(root, space="  ")
    xml_bytes = ET.tostring(root, encoding="utf-8", xml_declaration=True)
    # Re-parse generated bytes, then independently check the final coverage/counts.
    final_cases = list(ET.fromstring(xml_bytes).iter("test-case"))
    require(len(final_cases) == len(selected) and {c.get("fullname") for c in final_cases} == set(selected),
            "Generated XML lost or duplicated a case")
    require(all(c.get("result") == "Passed" for c in final_cases), "Generated XML contains a non-passing case")
    diagnostic_reports = []
    for run in diagnostics:
        diagnostic_reports.append({**source_record(run), "included_in_aggregate": False,
                                   "purpose": "Separate stage diagnostic; not additional regression pass coverage",
                                   "cases": [case_record(case, run) | {"fullname": name}
                                             for name, case in sorted(run["cases"].items())]})
    selected_source_counts = Counter(run["path"].name for _, run in selected.values())
    provenance = {
        "schema_version": 1, "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "description": "Case-level aggregate from separately completed NUnit runs; latest supplied case result wins.",
        "aggregate": {"file": "Tests.xml", "sha256": hashlib.sha256(xml_bytes).hexdigest(),
                      "total": len(selected), "passed": len(selected), "failed": 0,
                      "baseline_cases": EXPECTED_BASE_COUNT, "baseline_missing": missing_base,
                      "added_cases": sorted(set(selected) - set(runs[0]["cases"])),
                      "duration_seconds": float(aggregate_stats["duration"]),
                      "duration_meaning": "Sum of selected case durations, not one uninterrupted run",
                      "selected_case_counts_by_source": dict(selected_source_counts)},
        "sources_in_merge_order": [source_record(run) for run in runs],
        "merge_steps": merge_steps, "cases": provenance_cases, "separate_diagnostics": diagnostic_reports,
    }
    lines = ["NUnit aggregate from separate completed runs",
             f"Selected results: {len(selected)} passed, 0 failed, 0 skipped, 0 inconclusive.",
             f"Baseline coverage: {EXPECTED_BASE_COUNT}/{EXPECTED_BASE_COUNT}; added cases: {len(selected) - EXPECTED_BASE_COUNT}.",
             "Duration is the sum of selected case durations across the source runs.", "", "Source runs:"]
    for run in runs:
        lines.append(f"- {run['path'].name}: original result={run['root'].get('result')}, "
                     f"cases={len(run['cases'])}, selected={selected_source_counts[run['path'].name]}, SHA256={run['sha256']}")
        if run.get("note"):
            lines.append("  Run note: " + run["note"])
    superseded_failures = sum(entry["result"] == "Failed" for name, records in history.items() for entry in records[:-1])
    lines.extend(["", f"Superseded failed case results preserved in provenance: {superseded_failures}",
                  "Added UI regressions:"])
    lines.extend("- " + name for name in sorted(REQUIRED_UI_CASES))
    lines.extend(["", "Separate diagnostics (excluded from aggregate pass counts):"])
    lines.extend(f"- {run['path'].name}: {run['root'].get('result')}, {len(run['cases'])} case(s)" for run in diagnostics)
    if not diagnostics:
        lines.append("- None supplied.")
    lines.extend(["", "Case-by-case source selection and complete supplied run history: TestProvenance.json", ""])
    verify_unchanged(runs + diagnostics)
    publish({"Tests.xml": xml_bytes, "Tests.log": "\n".join(lines).encode("utf-8"),
             "TestProvenance.json": (json.dumps(provenance, indent=2, ensure_ascii=False) + "\n").encode("utf-8")})
    print(f"Published {len(selected)} passing cases from {len(runs)} separate runs; "
          f"{len(diagnostics)} diagnostic run(s) kept outside the aggregate.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (InvalidResults, OSError) as error:
        print(f"Aggregation rejected: {error}", file=sys.stderr)
        raise SystemExit(2)
