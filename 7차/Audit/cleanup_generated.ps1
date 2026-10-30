$ErrorActionPreference = 'Stop'
$releaseRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$expectedRoot = 'C:\Users\PUVKID\Desktop\Hole It\7차'
if ($releaseRoot -ne $expectedRoot) { throw 'Unexpected release directory.' }
$facts = Get-Content -LiteralPath (Join-Path $releaseRoot 'SHA256.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$apkPath = Join-Path $releaseRoot 'HoleIt_7.apk'
$zipPath = Join-Path $releaseRoot 'HoleIt_7차_APK_Source.zip'
if ((Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash -ne $facts.apkSHA256) { throw 'Delivered APK checksum mismatch.' }
if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash -ne $facts.zipSHA256) { throw 'Delivered ZIP checksum mismatch.' }
$sourceRoot = (Resolve-Path -LiteralPath (Join-Path $releaseRoot 'Source')).Path
if ((Get-Item -LiteralPath $sourceRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Source must be a physical directory.' }
$freeBefore = (Get-PSDrive C).Free
$removed = @()
foreach ($folderName in @('Library', 'Temp', 'Logs', 'Builds', '.utmp')) {
    $targetPath = Join-Path $sourceRoot $folderName
    if (!(Test-Path -LiteralPath $targetPath)) { continue }
    $resolvedTarget = (Resolve-Path -LiteralPath $targetPath).Path
    if ($resolvedTarget -ne ($sourceRoot + '\' + $folderName) -or !$resolvedTarget.StartsWith($sourceRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup target escaped Source.' }
    if ((Get-Item -LiteralPath $resolvedTarget).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Cleanup target is a reparse point.' }
    Remove-Item -LiteralPath $resolvedTarget -Recurse
    $removed += $resolvedTarget
}
$result = [pscustomobject]@{
    removedGeneratedDirectories = $removed
    freedBytes = (Get-PSDrive C).Free - $freeBefore
    previousEditionsPreserved = $true
    originalInputsPreserved = $true
    deliveredApkSha256 = $facts.apkSHA256
    deliveredZipSha256 = $facts.zipSHA256
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'FinalCleanup.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 4

