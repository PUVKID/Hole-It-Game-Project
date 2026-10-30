"""Record C# source state before a fresh build and bind it to proven completed APK output.
This tool never builds or packages the app. Run snapshot before launching the fresh build,
complete only after it exits successfully, and verify before packaging.
"""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import re
import time
import uuid

MANIFEST_NAME = 'SourceBuildManifest.json'

def sha(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda: stream.read(4 * 1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()

def utc_now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()

def source_hashes(release):
    source = Path(release) / 'Source'
    files = sorted(path for folder in ('Assets', 'Packages')
                   for path in (source / folder).rglob('*.cs') if path.is_file())
    assert files, 'No shipped C# sources found'
    return {path.relative_to(source).as_posix(): sha(path) for path in files}

def hashes_digest(hashes):
    return hashlib.sha256(json.dumps(hashes, sort_keys=True, separators=(',', ':')).encode('utf-8')).hexdigest()

def require_current_sources(release, manifest):
    actual = source_hashes(release)
    expected = manifest['sourceCsSha256']
    changes = sorted(name for name in set(actual) | set(expected) if actual.get(name) != expected.get(name))
    assert not changes, 'C# sources changed after the pre-build snapshot: ' + ', '.join(changes[:12])
    assert hashes_digest(actual) == manifest['sourceCsDigest'], 'Source manifest digest mismatch'
    return actual

def build_completion_evidence(release, apk_path, started_ns):
    release, apk_path = Path(release), Path(apk_path)
    assert apk_path.is_file(), 'Completed APK is missing'
    assert apk_path.stat().st_mtime_ns >= started_ns, 'APK predates this pre-build source snapshot'
    apk_hash = sha(apk_path)
    unity_report = release / 'Source/TestResults/AndroidBuild.txt'
    assert unity_report.is_file(), 'Unity Android build report is missing'
    unity = unity_report.read_text(encoding='utf-8-sig')
    lines = [line.strip() for line in unity.splitlines()]
    statuses = [line for line in lines if line in ('Succeeded', 'Failed', 'Cancelled', 'Unknown')]
    errors = [line for line in lines if line.startswith('Errors:')]
    if statuses == ['Succeeded'] and errors == ['Errors: 0']:
        route = 'unity'
        records = [unity_report, release / 'Audit/AndroidBuild.log']
    else:
        report = release / 'Source/TestResults/AndroidGradleBuild.txt'
        log = release / 'Audit/AndroidGradleBuild.log'
        assert report.is_file() and log.is_file(), 'Successful Gradle recovery evidence is missing'
        text = '\n'.join(report.read_text(encoding='utf-8-sig').splitlines())
        log_lines = [line.strip() for line in log.read_text(encoding='utf-8-sig').splitlines()]
        success = re.findall(r'^Gradle assembleRelease:[ \t]*(\S+)[ \t]*$', text, re.MULTILINE)
        codes = re.findall(r'^Gradle exit code:[ \t]*(-?\d+)[ \t]*$', text, re.MULTILINE)
        hashes = re.findall(r'^APK SHA256:[ \t]*([0-9a-fA-F]{64})[ \t]*$', text, re.MULTILINE)
        assert (success == ['Succeeded'] and codes == ['0'] and len(hashes) == 1
                and hashes[0].lower() == apk_hash
                and any(re.fullmatch(r'BUILD SUCCESSFUL(?: in .+)?', line) for line in log_lines)
                and not any(re.fullmatch(r'BUILD FAILED(?: in .+)?', line) for line in log_lines)), 'Gradle recovery did not prove this APK completed successfully'
        route = 'gradle-recovery'
        records = [unity_report, report, log]
    for path in records:
        assert path.is_file() and path.stat().st_size > 0, 'Build evidence missing: ' + str(path)
        assert path.stat().st_mtime_ns >= started_ns, 'Build evidence predates this snapshot: ' + str(path)
    return {'route': route, 'apkSHA256': apk_hash,
            'records': [{'path': path.relative_to(release).as_posix(), 'sha256': sha(path)} for path in records]}

def create_snapshot(release):
    release = Path(release)
    path = release / 'Audit' / MANIFEST_NAME
    if path.exists():
        previous = json.loads(path.read_text(encoding='utf-8-sig'))
        historical = path.with_name('SourceBuildManifest-history-' + str(previous.get('buildId', uuid.uuid4().hex)) + '.json')
        assert not historical.exists(), 'Historical manifest already exists; inspect before resnapshotting'
        historical.write_bytes(path.read_bytes())
    hashes = source_hashes(release)
    manifest = {'schemaVersion': 1, 'status': 'prepared', 'buildId': uuid.uuid4().hex,
                'createdUtc': utc_now(), 'createdNs': time.time_ns(),
                'sourceCsScope': 'All shipped .cs files under Source/Assets and Source/Packages; excludes Library/build outputs. This is not a byte-for-byte asset or runtime equivalence claim.',
                'sourceCsSha256': hashes, 'sourceCsDigest': hashes_digest(hashes),
                'historicalInputs': 'VerificationAtNativeCompile.cs.txt and VerificationSnapshotDifference.* describe discarded pre-fix build inputs, not this build.',
                'completion': None}
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    return manifest

def complete_manifest(release, apk_path=None):
    release = Path(release); path = release / 'Audit' / MANIFEST_NAME
    manifest = json.loads(path.read_text(encoding='utf-8-sig'))
    assert manifest.get('schemaVersion') == 1 and manifest.get('status') == 'prepared', 'Take a fresh pre-build snapshot first'
    require_current_sources(release, manifest)
    apk_path = Path(apk_path) if apk_path else release / 'Source/Builds/Android/HoleIt.apk'
    evidence = build_completion_evidence(release, apk_path, manifest['createdNs'])
    manifest['status'] = 'completed'; manifest['completedUtc'] = utc_now(); manifest['completion'] = evidence
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    return manifest

def verify_completed_manifest(release, apk_path=None):
    release = Path(release); path = release / 'Audit' / MANIFEST_NAME
    assert path.is_file(), 'SourceBuildManifest.json missing: snapshot before the fresh build, then complete after success'
    manifest = json.loads(path.read_text(encoding='utf-8-sig'))
    assert manifest.get('schemaVersion') == 1 and manifest.get('status') == 'completed', 'Fresh build source manifest is not completed'
    require_current_sources(release, manifest)
    apk_path = Path(apk_path) if apk_path else release / 'Source/Builds/Android/HoleIt.apk'
    evidence = build_completion_evidence(release, apk_path, manifest['createdNs'])
    assert evidence == manifest['completion'], 'Completed APK/build evidence changed after manifest completion'
    return manifest

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('snapshot', 'complete', 'verify'))
    parser.add_argument('--release', type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument('--apk', type=Path)
    args = parser.parse_args()
    if args.action == 'snapshot':
        assert args.apk is None, '--apk is not used by a pre-build snapshot'
        manifest = create_snapshot(args.release)
    elif args.action == 'complete':
        manifest = complete_manifest(args.release, args.apk)
    else:
        manifest = verify_completed_manifest(args.release, args.apk)
    print(json.dumps({'status': manifest['status'], 'buildId': manifest['buildId'],
                      'sourceCsFiles': len(manifest['sourceCsSha256']),
                      'sourceCsDigest': manifest['sourceCsDigest'],
                      'apkSHA256': (manifest.get('completion') or {}).get('apkSHA256')}, ensure_ascii=False))

if __name__ == '__main__':
    main()