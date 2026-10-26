param([string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe")
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityPath)) { throw 'Unity 6000.3.6f1 with Android Build Support is required.' }
# The junction keeps build tools on an ASCII path without copying the source.
$buildAlias = Join-Path $env:TEMP ('HoleItBuild-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Junction -Path $buildAlias -Target $PSScriptRoot | Out-Null
$previousBeeThreads = [Environment]::GetEnvironmentVariable('BEE_BUILD_THREADS', 'Process')
try {
    # Bound native compiler concurrency without changing the generated game's optimization.
    if ([string]::IsNullOrWhiteSpace($previousBeeThreads)) { $env:BEE_BUILD_THREADS = '8' }
    $buildLog = Join-Path $PSScriptRoot 'AndroidBuild.log'
    $arguments = '-batchmode -buildTarget Android -projectPath "' + $buildAlias + '" -executeMethod ReferenceBuilder.BuildAndroid -logFile "' + $buildLog + '"'
    $buildProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
    if ($buildProcess.ExitCode -ne 0) { throw ('Build failed. See: ' + $buildLog) }
    $apkPath = Join-Path $PSScriptRoot 'Builds\Android\HoleIt.apk'
    if (!(Test-Path -LiteralPath $apkPath)) { throw 'APK was not generated.' }
    if ((Get-Item -LiteralPath $apkPath).Length -lt 500000000) { throw 'APK did not meet the 500 MB requirement.' }
    Write-Host ('APK generated: ' + $apkPath)
} finally {
    [Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS', $previousBeeThreads, 'Process')
    $aliasItem = Get-Item -LiteralPath $buildAlias -ErrorAction SilentlyContinue
    if ($aliasItem -and $aliasItem.LinkType -eq 'Junction' -and $aliasItem.Target -eq $PSScriptRoot) {
        $aliasItem.Delete()
    }
}
