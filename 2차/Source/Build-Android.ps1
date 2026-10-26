param([string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe")
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityPath)) {
    throw 'Unity 6000.3.6f1 경로를 -UnityPath 인수로 지정하세요. Android Build Support가 필요합니다.'
}
$projectRoot = $PSScriptRoot
$buildLog = Join-Path $projectRoot 'AndroidBuild.log'
$arguments = '-batchmode -diag-debug-shader-compiler -buildTarget Android -projectPath "' + $projectRoot + '" -executeMethod AndroidRelease.Build -logFile "' + $buildLog + '"'
$buildProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
if ($buildProcess.ExitCode -ne 0) { throw ('빌드 실패. 로그: ' + $buildLog) }
$apkPath = Join-Path $projectRoot 'Builds\Android\HoleIt.apk'
if (!(Test-Path -LiteralPath $apkPath)) { throw 'APK 파일이 생성되지 않았습니다.' }
Write-Host ('APK 생성 완료: ' + $apkPath)

