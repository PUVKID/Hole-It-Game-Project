$ErrorActionPreference = 'Stop'
$taskRelease = 'C:\Users\PUVKID\Desktop\Hole It\7차'
$taskSource = Join-Path $taskRelease 'Source'
$taskAlias = $env:HOLEIT_WORKSPACE_ALIAS
if ([string]::IsNullOrWhiteSpace($taskAlias)) { throw 'HOLEIT_WORKSPACE_ALIAS 환경변수에 작업 경로를 지정해야 합니다.' }
$taskAliasItem = Get-Item -LiteralPath $taskAlias
if ($taskAliasItem.Target -ne $taskSource) { throw 'Unexpected build alias target.' }
$taskGradle = Join-Path $taskAlias 'Library\Bee\Android\Prj\IL2CPP\Gradle'
$taskAndroidRoot = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$taskJava = Join-Path $taskAndroidRoot 'OpenJDK\bin\java.exe'
$taskLauncher = Join-Path $taskAndroidRoot 'Tools\gradle\lib\gradle-launcher-8.13.jar'
$taskOutput = Join-Path $taskGradle 'launcher\build\outputs\apk\release\launcher-release.apk'
$taskDestination = Join-Path $taskSource 'Builds\Android\HoleIt.apk'
$taskLog = Join-Path $PSScriptRoot 'AndroidGradleBuild.log'
if (Test-Path -LiteralPath $taskDestination) { throw 'A retained APK already exists; verify it before another build.' }
if (Test-Path -LiteralPath $taskOutput) { throw 'A Gradle APK already exists; inspect it before another build.' }
if ((Get-PSDrive C).Free -lt 1850000000) { throw 'Need at least 1.85 GB free for the final packaging retry.' }
$taskNativeEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'GradleRecoveryCacheCleanup.json') -Raw | ConvertFrom-Json
foreach ($taskInput in $taskNativeEvidence.PreservedGradleNativeInputs) {
    if ((Get-FileHash -LiteralPath $taskInput.Path -Algorithm SHA256).Hash -ne $taskInput.SHA256) { throw 'A preserved native input changed.' }
}
if (Test-Path -LiteralPath $taskLog) {
    $taskPrevious = Join-Path $PSScriptRoot ('AndroidGradleBuild-before-final-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
    Copy-Item -LiteralPath $taskLog -Destination $taskPrevious
}
$taskOriginalDirectory = Get-Location
try {
    Set-Location -LiteralPath (Join-Path $taskGradle 'launcher')
    & $taskJava '-classpath' $taskLauncher 'org.gradle.launcher.GradleMain' '-Dorg.gradle.jvmargs=-Xmx4096m' '--no-daemon' '--max-workers=1' 'assembleRelease' 2>&1 | Tee-Object -FilePath $taskLog
    $taskExit = $LASTEXITCODE
} finally {
    Set-Location -LiteralPath $taskOriginalDirectory.Path
}
if ($taskExit -ne 0) { throw ('Gradle failed with exit ' + $taskExit + '; original evidence retained.') }
$taskLogText = Get-Content -LiteralPath $taskLog -Raw
if (!$taskLogText.Contains('BUILD SUCCESSFUL') -or $taskLogText.Contains('BUILD FAILED')) { throw 'Gradle success evidence is incomplete.' }
if ((Get-Item -LiteralPath $taskOutput).Length -lt 500000000) { throw 'APK does not meet the requested minimum size.' }
New-Item -ItemType Directory -Path (Split-Path -Parent $taskDestination) -Force | Out-Null
Move-Item -LiteralPath $taskOutput -Destination $taskDestination
$taskHash = (Get-FileHash -LiteralPath $taskDestination -Algorithm SHA256).Hash.ToLowerInvariant()
$taskBytes = (Get-Item -LiteralPath $taskDestination).Length
@(
    'Build route: completed Unity-generated Gradle project after Unity packaging failed due to disk space'
    'Gradle assembleRelease: Succeeded'
    'Gradle exit code: 0'
    ('APK bytes: ' + $taskBytes)
    ('APK SHA256: ' + $taskHash)
) | Set-Content -LiteralPath (Join-Path $taskSource 'TestResults\AndroidGradleBuild.txt') -Encoding UTF8
[pscustomobject]@{ APK=$taskDestination; Bytes=$taskBytes; SHA256=$taskHash; GradleExitCode=$taskExit } | ConvertTo-Json
