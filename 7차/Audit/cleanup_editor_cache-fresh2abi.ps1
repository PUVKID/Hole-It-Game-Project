[CmdletBinding()]
param([switch]$RecordEvidence,[switch]$Apply)
. (Join-Path $PSScriptRoot 'fresh2abi_recovery_guard.ps1')
if ($RecordEvidence -and $Apply) { throw 'Record and review evidence separately before using -Apply.' }
if ($RecordEvidence) {
    $taskEvidence=Get-TaskRecoveryEvidence -Record
    [pscustomobject]@{Action='Evidence recorded only; nothing deleted';BuildId=$taskEvidence.BuildId;NativeInputs=$taskEvidence.NativeInputs.Count;Evidence=$taskEvidencePath}|ConvertTo-Json
    return
}
$taskEvidence=Get-TaskRecoveryEvidence
# Preserve Bee, build profiles, editor state and PackageCache. These are regenerable caches.
$taskNames=@('Artifacts','TempArtifacts','ArtifactDB','ArtifactDB-lock','SourceAssetDB','SourceAssetDB-lock','ScriptAssemblies','ShaderCache','ShaderCache.db','PlayerDataCache','BuildPlayerData','SplashScreenCache')
$taskTargets=@()
foreach($taskName in $taskNames) {
    $taskCandidate=Join-Path $taskLibrary $taskName
    if (!(Test-Path -LiteralPath $taskCandidate)) { continue }
    $taskResolved=Assert-TaskPhysicalPath $taskCandidate $taskLibrary
    if ((Split-Path -Parent $taskResolved) -ne $taskLibrary) { throw 'Target escaped direct allowed Library children.' }
    $taskItem=Get-Item -LiteralPath $taskResolved -Force
    if ($taskItem.PSIsContainer) {
        $taskLinks=@(Get-ChildItem -LiteralPath $taskResolved -Force -Recurse | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint})
        if ($taskLinks.Count -gt 0) { throw "Refusing recursive delete with reparse-point descendant: $taskResolved" }
    }
    $taskTargets+=$taskResolved
}
$taskPlan=[pscustomobject]@{Mode=if($Apply){'Apply'}else{'Read-only plan'};BuildId=$taskEvidence.BuildId;Targets=$taskTargets;PreservedGradle=$taskGradle;PreservedNativeInputs=$taskEvidence.NativeInputs;FreeBefore=(Get-PSDrive C).Free}
if (!$Apply) { $taskPlan|ConvertTo-Json -Depth 7; return }
Assert-TaskStopped
$taskPlanPath=Join-Path $PSScriptRoot ('EditorCacheCleanup-fresh2abi-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
$taskPlan|ConvertTo-Json -Depth 7|Set-Content -LiteralPath $taskPlanPath -Encoding UTF8
$taskRemoved=@()
foreach($taskTarget in $taskTargets) {
    Assert-TaskStopped
    $null=Assert-TaskPhysicalPath $taskTarget $taskLibrary
    Remove-Item -LiteralPath $taskTarget -Recurse -Force
    $taskRemoved+=$taskTarget
}
$null=Get-TaskRecoveryEvidence
[pscustomobject]@{Status='Completed';BuildId=$taskEvidence.BuildId;Removed=$taskRemoved;Plan=$taskPlanPath;FreshEvidence=$taskEvidencePath;FreeAfter=(Get-PSDrive C).Free}|
    ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'EditorCacheCleanup-fresh2abi-completed.json') -Encoding UTF8
Get-PSDrive C|Select-Object Free
