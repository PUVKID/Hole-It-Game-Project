$ErrorActionPreference = 'Stop'
$releaseRoot = Split-Path -Parent $PSScriptRoot
$apkPath = Join-Path $releaseRoot 'Source\Builds\Android\HoleIt.apk'
$toolApkPath = 'C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work\Build7\Builds\Android\HoleIt.apk'
if ((Get-Item -LiteralPath (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $toolApkPath)))).Target -ne (Join-Path $releaseRoot 'Source')) { throw 'Unexpected ASCII build alias.' }
$androidRoot = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$buildTools = Join-Path $androidRoot 'SDK\build-tools\36.0.0'
if ((Get-Item -LiteralPath $apkPath).Length -lt 500000000) { throw 'APK is under 500 MB.' }
$signature = & (Join-Path $androidRoot 'OpenJDK\bin\java.exe') -jar (Join-Path $buildTools 'lib\apksigner.jar') verify --verbose --print-certs $apkPath 2>&1
if ($LASTEXITCODE -ne 0) { throw ($signature | Out-String) }
$signature | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'ApkSignature.txt') -Encoding UTF8
if (($signature | Out-String) -notmatch '03b2e61e715a8e2fd3854fb1acddf905a453766389b9765f1e15281b4767c5fa') { throw 'Signing certificate differs from edition 5.' }
$manifest = & (Join-Path $buildTools 'aapt.exe') dump badging $toolApkPath 2>&1
if ($LASTEXITCODE -ne 0) { throw ($manifest | Out-String) }
$manifest | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'ApkManifest.txt') -Encoding UTF8
$manifestText = $manifest | Out-String
foreach ($expected in @("package: name='com.pocketplayground.pocketpit'", "versionCode='9'", "versionName='7.0.0'", "application-label:'Hole It!'", "sdkVersion:'26'", "targetSdkVersion:'36'")) {
    if (!$manifestText.Contains($expected)) { throw ('Manifest mismatch: ' + $expected) }
}
# Require exactly the two intended phone ABIs; no unused x86 emulator libraries.
$nativeLines = @($manifest | Where-Object { $_ -match '^native-code:' })
if ($nativeLines.Count -ne 1) { throw 'Expected exactly one native-code declaration.' }
$actualAbis = @([regex]::Matches([string]$nativeLines[0], "'([^']+)'") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
$expectedAbis = @('armeabi-v7a', 'arm64-v8a')
$abiDifference = @(Compare-Object -ReferenceObject $expectedAbis -DifferenceObject $actualAbis -CaseSensitive)
if ($actualAbis.Count -ne 2 -or $abiDifference.Count -ne 0) { throw ('APK ABI mismatch: ' + ($actualAbis -join ', ') + '; required exactly armeabi-v7a and arm64-v8a.') }
$alignment = & (Join-Path $buildTools 'zipalign.exe') -c -P 16 -v 4 $toolApkPath 2>&1
if ($LASTEXITCODE -ne 0) { throw ($alignment | Out-String) }
$alignment | Select-Object -Last 2 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'ApkAlignment.txt') -Encoding UTF8
Write-Output ('APK verified: ' + (Get-Item -LiteralPath $apkPath).Length + ' bytes')
Write-Output ($manifest | Select-Object -First 4)
Write-Output ($signature | Select-String 'Verified using|SHA-256 digest')

