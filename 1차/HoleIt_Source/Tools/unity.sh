#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ACTION="${1:-validate}"
UNITY="${UNITY_EDITOR:-}"
if [[ -z "$UNITY" || ! -x "$UNITY" ]]; then
  echo 'BLOCKED: set UNITY_EDITOR to an installed Unity 6000.3.6f1 Editor executable.' >&2
  exit 69
fi
mkdir -p "$ROOT/TestResults"
case "$ACTION" in
 validate) exec "$UNITY" -batchmode -nographics -projectPath "$ROOT" -executeMethod HoleItProjectValidation.ValidateCli -logFile "$ROOT/TestResults/validate.log" ;;
 editmode) exec "$UNITY" -batchmode -projectPath "$ROOT" -runTests -testPlatform EditMode -testFilter CampaignEditModeTests -testResults "$ROOT/TestResults/EditMode.xml" -logFile "$ROOT/TestResults/EditMode.log" ;;
 playmode) exec "$UNITY" -batchmode -projectPath "$ROOT" -runTests -testPlatform PlayMode -testFilter CampaignPlayModeTests -testResults "$ROOT/TestResults/PlayMode.xml" -logFile "$ROOT/TestResults/PlayMode.log" ;;
 android) exec "$UNITY" -batchmode -nographics -buildTarget Android -projectPath "$ROOT" -executeMethod HoleItBuildScript.BuildAndroid -logFile "$ROOT/TestResults/android.log" ;;
 rebuild) exec "$UNITY" -batchmode -projectPath "$ROOT" -executeMethod HoleItBuilder.BuildAllCli -logFile "$ROOT/TestResults/rebuild.log" ;;
 *) echo 'Usage: Tools/unity.sh validate|editmode|playmode|android|rebuild' >&2; exit 64 ;;
esac
