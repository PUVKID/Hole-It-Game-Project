$ErrorActionPreference = 'Stop'
$taskSource = 'C:\Users\PUVKID\Desktop\Hole It\7차\Source'
$taskAlias = $env:HOLEIT_WORKSPACE_ALIAS
if ([string]::IsNullOrWhiteSpace($taskAlias)) { throw 'HOLEIT_WORKSPACE_ALIAS 환경변수에 작업 경로를 지정해야 합니다.' }
$taskLibrary = Join-Path $taskSource 'Library'
$taskGradle = Join-Path $taskLibrary 'Bee\Android\Prj\IL2CPP\Gradle'
if ((Resolve-Path -LiteralPath $taskSource).Path -ne $taskSource) { throw 'Unexpected source root.' }
if ((Get-Item -LiteralPath $taskSource).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Physical source required.' }
$taskRunning = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -ieq 'Unity.exe' -and ($_.CommandLine -like ('*' + $taskSource + '*') -or $_.CommandLine -like ('*' + $taskAlias + '*'))
}
if ($taskRunning) { throw 'This project still has a Unity editor running.' }
if (!(Test-Path -LiteralPath (Join-Path $taskGradle 'settings.gradle'))) { throw 'Generated Gradle project missing.' }
$taskNative = @(Get-ChildItem -LiteralPath (Join-Path $taskGradle 'unityLibrary\src\main\jniLibs') -Filter '*.so' -File -Recurse | ForEach-Object {
    [pscustomobject]@{ Path=$_.FullName; Bytes=$_.Length; SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
if ($taskNative.Count -ne 9) { throw 'Expected nine preserved native JNI inputs.' }
$taskFreeBefore = (Get-PSDrive C).Free
$taskRemoved = @()
foreach ($taskChild in Get-ChildItem -LiteralPath $taskLibrary -Force) {
    if ($taskChild.Name -eq 'Bee') { continue }
    $taskTarget = (Resolve-Path -LiteralPath $taskChild.FullName).Path
    if (!$taskTarget.StartsWith($taskLibrary + '\', [StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Parent $taskTarget) -ne $taskLibrary) { throw 'Target escaped Library.' }
    if ($taskChild.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing reparse-point target.' }
    Remove-Item -LiteralPath $taskTarget -Recurse
    $taskRemoved += $taskTarget
}
foreach ($taskInput in $taskNative) {
    if ((Get-FileHash -LiteralPath $taskInput.Path -Algorithm SHA256).Hash -ne $taskInput.SHA256) { throw 'Preserved native input changed.' }
}
[pscustomobject]@{
    Reason='Completed Editor tests; remove regenerable Editor Library caches while preserving the completed Android Gradle project and native inputs.'
    Removed=$taskRemoved
    PreservedBeeRoot=(Join-Path $taskLibrary 'Bee')
    PreservedNativeInputs=$taskNative
    FreeBefore=$taskFreeBefore
    FreeAfter=(Get-PSDrive C).Free
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'EditorCacheCleanup.json') -Encoding UTF8
Get-PSDrive C | Select-Object Free
