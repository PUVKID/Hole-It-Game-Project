[CmdletBinding()]
param([switch]$RecordEvidence,[switch]$Execute)
. (Join-Path $PSScriptRoot 'fresh2abi_recovery_guard.ps1')
if($RecordEvidence -and $Execute){throw 'Record/review fresh input hashes separately before executing Gradle.'}
if($RecordEvidence){$taskEvidence=Get-TaskRecoveryEvidence -Record;[pscustomobject]@{Action='Fresh evidence recorded only';BuildId=$taskEvidence.BuildId;NativeInputs=$taskEvidence.NativeInputs.Count;GradleInputs=$taskEvidence.GradleInputs.Count}|ConvertTo-Json;return}
$taskEvidence=Get-TaskRecoveryEvidence
$taskAndroidRoot='C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$taskJava=Join-Path $taskAndroidRoot 'OpenJDK\bin\java.exe'
$taskLauncher=Join-Path $taskAndroidRoot 'Tools\gradle\lib\gradle-launcher-8.13.jar'
$taskOutput=Join-Path $taskGradle 'launcher\build\outputs\apk\release\launcher-release.apk'
$taskDestination=Join-Path $taskSource 'Builds\Android\HoleIt.apk'
$taskLog=Join-Path $PSScriptRoot 'AndroidGradleBuild.log'
foreach($taskExisting in @($taskOutput,$taskDestination)) { if(Test-Path -LiteralPath $taskExisting){throw "APK already exists; inspect it explicitly, never overwrite: $taskExisting"} }
if ((Get-PSDrive C).Free -lt 2000000000) { throw 'Need at least 2.0 GB free for this bounded packaging retry.' }
foreach($taskTool in @($taskJava,$taskLauncher)) { if(!(Test-Path -LiteralPath $taskTool)){throw "Bundled tool missing: $taskTool"} }
if (!$Execute) {
    [pscustomobject]@{Action='Validated only; Gradle was not launched';BuildId=$taskEvidence.BuildId;JNIInputs=6;Destination=$taskDestination;Next='Use -Execute only after reviewing evidence and free space.'}|ConvertTo-Json
    return
}
if(Test-Path -LiteralPath $taskLog) { Copy-Item -LiteralPath $taskLog -Destination (Join-Path $PSScriptRoot ('AndroidGradleBuild-before-fresh2abi-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')) }
Assert-TaskStopped
$taskStart=[DateTimeOffset]::UtcNow
$taskOriginalDirectory=Get-Location
$taskExit=-1
try {
    Set-Location -LiteralPath (Join-Path $taskAlias 'Library\Bee\Android\Prj\IL2CPP\Gradle\launcher')
    & $taskJava '-classpath' $taskLauncher 'org.gradle.launcher.GradleMain' '-Dorg.gradle.jvmargs=-Xmx4096m' '--no-daemon' '--max-workers=1' 'assembleRelease' 2>&1|
        Out-File -LiteralPath $taskLog -Encoding UTF8
    $taskExit=$LASTEXITCODE
} finally { Set-Location -LiteralPath $taskOriginalDirectory.Path }
if($taskExit -ne 0) { throw "Gradle recovery failed with exit $taskExit; no automatic retry/deletion/publication." }
$taskText=Get-Content -LiteralPath $taskLog -Raw -Encoding UTF8
if($taskText -notmatch '(?m)^BUILD SUCCESSFUL(?: in .+)?\s*$' -or $taskText -match '(?m)^BUILD FAILED') { throw 'Gradle success evidence incomplete.' }
$null=Assert-TaskPhysicalPath $taskOutput $taskGradle
$taskApk=Get-Item -LiteralPath $taskOutput
if($taskApk.Length -lt 500000000 -or $taskApk.LastWriteTimeUtc -lt $taskStart.UtcDateTime) { throw 'APK stale or below requested 500 MB.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskZip=[IO.Compression.ZipFile]::OpenRead($taskOutput)
try {
    $taskEntries=@($taskZip.Entries|Where-Object{$_.FullName -match '^lib/.+\.so$'})
    $taskExpected=@(foreach($abi in @('armeabi-v7a','arm64-v8a')){foreach($name in @('libil2cpp.so','libmain.so','libunity.so')){"lib/$abi/$name"}})
    foreach($taskRequired in $taskExpected){if(!$taskZip.GetEntry($taskRequired)){throw "Core JNI input missing from APK: $taskRequired"}}
    if(@($taskEntries|Where-Object{$_.FullName -notmatch '^lib/(armeabi-v7a|arm64-v8a)/lib(il2cpp|main|unity|game|swappywrapper|c\+\+_shared)\.so$' -or $_.Length -lt 1024}).Count -ne 0){throw 'Unexpected packaged ABI or native library.'}
    foreach($name in @('AndroidManifest.xml','classes.dex')){if(!$taskZip.GetEntry($name)){throw "Required APK entry missing: $name"}}
    foreach($entry in $taskEntries) {
        $stream=$entry.Open();try{$head=New-Object byte[] 20;if($stream.Read($head,0,20) -ne 20){throw 'Truncated native APK entry'}}finally{$stream.Dispose()}
        $arm64=$entry.FullName.StartsWith('lib/arm64-v8a/');$class=if($arm64){2}else{1};$machine=if($arm64){183}else{40}
        if($head[0] -ne 127 -or $head[1] -ne 69 -or $head[2] -ne 76 -or $head[3] -ne 70 -or $head[4] -ne $class -or [BitConverter]::ToUInt16($head,18) -ne $machine){throw 'Packaged native ELF ABI mismatch.'}
    }
} finally { $taskZip.Dispose() }
$null=Get-TaskRecoveryEvidence
$taskHash=Get-TaskHash $taskOutput;$taskBytes=$taskApk.Length
$taskDirectory=Split-Path -Parent $taskDestination
if(!(Test-Path -LiteralPath $taskDirectory)){New-Item -ItemType Directory -Path $taskDirectory -Force|Out-Null}
$null=Assert-TaskPhysicalPath $taskDirectory $taskSource
if(Test-Path -LiteralPath $taskDestination){throw 'Destination appeared during packaging; refusing overwrite.'}
Move-Item -LiteralPath $taskOutput -Destination $taskDestination
if((Get-TaskHash $taskDestination) -ne $taskHash){throw 'APK changed while being moved.'}
$taskReport=Join-Path $taskSource 'TestResults\AndroidGradleBuild.txt'
if(Test-Path -LiteralPath $taskReport){Copy-Item -LiteralPath $taskReport -Destination (Join-Path $PSScriptRoot ('AndroidGradleBuild-report-before-fresh2abi-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.txt'))}
@('Build route: unchanged fresh Unity-generated two-ABI Gradle project after documented packaging disk-space failure','Gradle assembleRelease: Succeeded','Gradle exit code: 0',('Build ID: '+$taskEvidence.BuildId),('APK bytes: '+$taskBytes),('APK SHA256: '+$taskHash),('Fresh input evidence: '+$taskEvidencePath))|
    Set-Content -LiteralPath $taskReport -Encoding UTF8
[pscustomobject]@{Status='Gradle packaging succeeded; Unity failure report preserved';BuildId=$taskEvidence.BuildId;APK=$taskDestination;Bytes=$taskBytes;SHA256=$taskHash;GradleExitCode=$taskExit;FreshEvidence=$taskEvidencePath;StartedUtc=$taskStart.ToString('O');CompletedUtc=[DateTimeOffset]::UtcNow.ToString('O')}|
    ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Fresh2AbiGradleRecoveryCompleted.json') -Encoding UTF8
# Parent completes SourceBuildManifest only after inspecting this evidence and APK.
[pscustomobject]@{APK=$taskDestination;Bytes=$taskBytes;SHA256=$taskHash;GradleExitCode=0}|ConvertTo-Json
