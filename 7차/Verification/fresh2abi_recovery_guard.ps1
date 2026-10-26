# Shared validation only. Dot-source from the two guarded recovery scripts.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$taskRelease = 'C:\Users\PUVKID\Desktop\Hole It\7차'
$taskSource = Join-Path $taskRelease 'Source'
$taskAlias = $env:HOLEIT_WORKSPACE_ALIAS
if ([string]::IsNullOrWhiteSpace($taskAlias)) { throw 'HOLEIT_WORKSPACE_ALIAS 환경변수에 작업 경로를 지정해야 합니다.' }
$taskLibrary = Join-Path $taskSource 'Library'
$taskGradle = Join-Path $taskLibrary 'Bee\Android\Prj\IL2CPP\Gradle'
$taskManifestPath = Join-Path $PSScriptRoot 'SourceBuildManifest.json'
$taskLaunchPath = Join-Path $PSScriptRoot 'FreshAndroidBuildLaunch.json'
$taskEvidencePath = Join-Path $PSScriptRoot 'Fresh2AbiRecoveryEvidence.json'
$taskExpectedBuildId = 'e4aee4b55b01409eaea73f79a47d6ebb'
$taskExpectedPid = 83004
$taskOriginalPid = 81424
$taskDaemonPath = Join-Path $PSScriptRoot 'Fresh2AbiFailedGradleDaemon.log'
$script:taskValidatedParentDirectories = $null

function Get-TaskHash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Convert-TaskDate($Value) {
    # PowerShell 7.5 may deserialize ISO JSON dates directly into DateTime.
    if($Value -is [DateTimeOffset]){return $Value}
    if($Value -is [DateTime]){return [DateTimeOffset]$Value}
    [DateTimeOffset]::Parse([string]$Value)
}
function Assert-TaskPhysicalPath([string]$Path,[string]$Root) {
    $full=[IO.Path]::GetFullPath($Path);$base=[IO.Path]::GetFullPath($Root).TrimEnd('\')
    if ($full -ne $base -and !$full.StartsWith($base+'\',[StringComparison]::OrdinalIgnoreCase)) { throw "Path escaped expected root: $full" }
    $current=$full;$first=$true;$validated=[Collections.Generic.List[string]]::new()
    while ($current.Length -ge $base.Length) {
        $cacheKey=$base+"`n"+$current
        # Always inspect each requested file/directory. Reuse only parent checks
        # already completed within this single evidence invocation and root.
        if(!$first -and $null-ne $script:taskValidatedParentDirectories -and $script:taskValidatedParentDirectories.Contains($cacheKey)){break}
        $item=Get-Item -LiteralPath $current -Force -ErrorAction Stop
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Refusing reparse-point path: $current" }
        if($item.PSIsContainer){$validated.Add($cacheKey)}
        if ($current -eq $base) { break };$current=Split-Path -Parent $current;$first=$false
    }
    if($null-ne $script:taskValidatedParentDirectories){foreach($directoryKey in $validated){$null=$script:taskValidatedParentDirectories.Add($directoryKey)}}
    $full
}
function Assert-TaskStopped {
    $processes=@(Get-CimInstance Win32_Process)
    foreach($proc in $processes) {
        if ($proc.ProcessId -eq $taskExpectedPid) { throw 'Fresh Unity PID still exists; wait for parent-confirmed exit.' }
        $cmd=[string]$proc.CommandLine
        $isBuild=$proc.Name -match '^(Unity|java|javaw|clang\+\+|clang|ld\.lld|lld|bee_backend|UnityLinker|il2cpp)\.exe$'
        $thisProject=$cmd.Contains($taskSource)-or $cmd.Contains($taskAlias)
        if (($isBuild -and $thisProject) -or ($proc.Name -match '^java(w)?\.exe$' -and $cmd -match 'GradleDaemon|org\.gradle\.launcher\.GradleMain')) { throw "Build/Gradle process still running: $($proc.ProcessId) $($proc.Name)" }
    }
}
function Assert-TaskManifest {
    $m=Get-Content -LiteralPath $taskManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($m.schemaVersion -ne 1 -or $m.status -ne 'prepared' -or $m.buildId -ne $taskExpectedBuildId) { throw 'Not the pinned fresh prepared SourceBuildManifest; historical/completed/resnapshotted builds are rejected.' }
    $expected=@($m.sourceCsSha256.PSObject.Properties)
    $actual=@(foreach($folder in @('Assets','Packages')){Get-ChildItem -LiteralPath (Join-Path $taskSource $folder) -Filter '*.cs' -File -Recurse})
    if ($actual.Count -ne $expected.Count) { throw 'Shipped C# file set changed since fresh build snapshot.' }
    foreach($entry in $expected){$path=Join-Path $taskSource $entry.Name;$null=Assert-TaskPhysicalPath $path $taskSource;if((Get-TaskHash $path)-ne $entry.Value){throw "C# differs from fresh build snapshot: $($entry.Name)"}}
    if ((Get-Content -LiteralPath (Join-Path $taskSource 'ProjectSettings\ProjectSettings.asset') -Raw -Encoding UTF8) -notmatch '(?m)^\s*AndroidTargetArchitectures:\s*3\s*$') { throw 'Expected only ARMv7|ARM64 architecture mask3.' }
    $m
}
function Assert-TaskFreshFailure {
    Assert-TaskStopped
    $null=Assert-TaskPhysicalPath $taskSource $taskSource
    $null=Assert-TaskPhysicalPath $taskGradle $taskSource
    $alias=Get-Item -LiteralPath $taskAlias -Force
    if (@($alias.Target).Count-ne 1 -or [IO.Path]::GetFullPath([string]$alias.Target)-ne $taskSource) { throw 'Unexpected ASCII alias target.' }
    $m=Assert-TaskManifest
    $launch=Get-Content -LiteralPath $taskLaunchPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if($launch.Pid-ne $taskOriginalPid -or $launch.IncrementalRetry.Pid-ne $taskExpectedPid -or $launch.IncrementalRetry.SourceChanges -ne $false -or !$launch.DiscardedOldGradleProject){throw 'Missing pinned fresh launch / discarded old Gradle-project evidence.'}
    if ((@($launch.Architectures | Sort-Object)-join ',')-ne 'arm64-v8a,armeabi-v7a'){throw 'Fresh launch must contain exactly two ARM ABIs.'}
    $initialStart=Convert-TaskDate $launch.Utc;$start=Convert-TaskDate $launch.IncrementalRetry.Utc;$prepared=Convert-TaskDate $m.createdUtc
    if($initialStart-lt $prepared -or ($initialStart-$prepared).TotalSeconds-gt 60 -or $start-lt $initialStart){throw 'Launch does not follow this fresh source snapshot.'}
    $logPath=Join-Path $PSScriptRoot 'AndroidBuild.log';$reportPath=Join-Path $taskSource 'TestResults\AndroidBuild.txt'
    foreach($file in @($logPath,$reportPath)){if(!(Test-Path -LiteralPath $file)-or (Get-Item -LiteralPath $file).LastWriteTimeUtc-lt $start.UtcDateTime){throw "Fresh completed build evidence missing: $file"}}
    $report=Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
    if($report-notmatch '(?m)^Failed\s*$'-or $report-notmatch '(?m)^Errors:\s*[1-9]\d*\s*$'){throw 'Recovery requires a completed Failed Unity report with errors; never infer success or recover a successful build.'}
    $reportDate=[DateTimeOffset]::Parse(($report-split '\r?\n')[0]);if($reportDate-lt $start){throw 'Build report is from an older run.'}
    $log=Get-Content -LiteralPath $logPath -Raw -Encoding UTF8
    if($log-notmatch ('"processId"\s*:\s*'+$taskExpectedPid+'\b')-or $log-notmatch '\[Physics::Module\] Cleanup current backend'){throw 'Fresh log lacks this process ID / terminal cleanup evidence.'}
    $invoke=[regex]::Match($log,'(?m)^Starting:.*org\.gradle\.launcher\.GradleMain.*assembleRelease.*$')
    if(!$invoke.Success){throw 'Fresh build never reached generated Gradle assembleRelease.'}
    $before=$log.Substring(0,$invoke.Index);$tail=$log.Substring($invoke.Index)
    $nativeErrors='(?im)(fatal error:|\bld\.lld:\s*error:|\bclang(?:\+\+)?:\s*error:|LLVM ERROR:|Stack dump:|error CS\d+|IL2CPP error|failed running il2cpp|Building Library[/\\]Bee.*failed|(?:linker|compiler) command failed)'
    if($before-match $nativeErrors -or $tail-match $nativeErrors){throw 'Native/compiler/C# error detected; generated Gradle cannot repair this build.'}
    if($tail-notmatch '(?m)^> Task :unityLibrary:mergeReleaseAssets FAILED\s*$'-or $tail-notmatch 'GradleInvokationException'){throw 'No concrete Gradle asset-packaging failure in fresh Unity log.'}
    if(!(Test-Path -LiteralPath $taskDaemonPath)){throw 'Matching Gradle daemon failure evidence missing.'}
    $daemon=Get-Content -LiteralPath $taskDaemonPath -Raw -Encoding UTF8
    if($daemon-notmatch 'Build\{id=c6cfdf27-c047-4459-86fb-bd6838d3c7be, currentDir='-or !$daemon.Contains($taskAlias+'\Library\Bee\Android\Prj\IL2CPP\Gradle\launcher')-or $daemon-notmatch '2026-10-13T19:50:20\.'-or $daemon-notmatch '(?m)^BUILD FAILED(?: in .+)?\s*$'){throw 'Daemon evidence does not identify the completed pinned retry.'}
    $disk='(?is)(No space left|not enough space|insufficient (?:disk )?space|ENOSPC|디스크 공간이 부족|disk (?:is )?full|failed writing to file:[^\r\n]*\(28\))'
    $failures=[regex]::Matches($daemon,"(?ms)^Execution failed for task '([^']+)'\.(.*?)(?=^\* Try:|^\* Exception is:|^Execution failed for task |\z)")
    if($failures.Count-eq 0){throw 'Missing concrete failed Gradle task evidence.'}
    foreach($f in $failures){if($f.Groups[1].Value-notmatch '^:(launcher|unityLibrary):(processReleaseResources|mergeExtDexRelease|mergeReleaseAssets|packageRelease|mergeReleaseJavaResource|mergeReleaseJniLibFolders|mergeReleaseNativeLibs|stripReleaseDebugSymbols)$'-or $f.Groups[2].Value-notmatch $disk){throw ('Non-packaging or non-disk-space Gradle failure: '+$f.Groups[1].Value)}}
    foreach($required in @('settings.gradle','build.gradle','gradle.properties','launcher\build.gradle','unityLibrary\build.gradle','launcher\src\main\AndroidManifest.xml','unityLibrary\src\main\AndroidManifest.xml')){$null=Assert-TaskPhysicalPath (Join-Path $taskGradle $required) $taskGradle}
    $gradleText=(Get-Content -LiteralPath (Join-Path $taskGradle 'launcher\build.gradle') -Raw -Encoding UTF8)+(Get-Content -LiteralPath (Join-Path $taskGradle 'unityLibrary\build.gradle') -Raw -Encoding UTF8)
    if($gradleText-match 'buildIl2Cpp|Il2CppOutputProject|abiFilters[^\r\n]*x86' -or $gradleText-notmatch 'armeabi-v7a' -or $gradleText-notmatch 'arm64-v8a'){throw 'Generated project would recompile IL2CPP or has unexpected ABIs.'}
    # Unity's generated GameActivity/FramePacing CMake bridge is legitimate Gradle work.
    # It uses preserved src/main/cpp inputs, not deleted Bee IL2CPP object caches.
    $cmake=Get-Content -LiteralPath (Join-Path $taskGradle 'unityLibrary\src\main\cpp\CMakeLists.txt') -Raw -Encoding UTF8
    if($cmake-notmatch 'add_subdirectory\(GameActivity\)'-or $cmake-notmatch 'add_subdirectory\(FramePacing\)'-or $cmake-match 'il2cpp|Bee[/\\]artifacts'){throw 'Unexpected native CMake inputs.'}
    [pscustomobject]@{Manifest=$m;Start=$start;NativeStart=$initialStart;LogPath=$logPath;ReportPath=$reportPath;FailedTasks=@($failures|ForEach-Object{$_.Groups[1].Value}|Sort-Object -Unique)}
}
function Get-TaskNativeInputs($Start) {
    $jni=Join-Path $taskGradle 'unityLibrary\src\main\jniLibs';$null=Assert-TaskPhysicalPath $jni $taskGradle
    $files=@(Get-ChildItem -LiteralPath $jni -Filter '*.so' -File -Recurse)
    $expected=@(foreach($abi in @('armeabi-v7a','arm64-v8a')){foreach($name in @('libil2cpp.so','libmain.so','libunity.so')){"$abi\$name"}})
    if($files.Count-ne 6){throw 'Expected exactly six current generated JNI inputs.'}
    foreach($f in $files){$null=Assert-TaskPhysicalPath $f.FullName $taskGradle;$relative=$f.FullName.Substring($jni.Length+1)
        if($relative-notin $expected -or $f.Length-lt 1024 -or $f.CreationTimeUtc-lt $Start.UtcDateTime){throw "Unexpected/stale native input: $relative"}
        if($f.Name-eq 'libil2cpp.so'-and $f.LastWriteTimeUtc-lt $Start.UtcDateTime){throw 'libil2cpp predates the fresh build.'}
        $stream=[IO.File]::OpenRead($f.FullName);try{$head=New-Object byte[] 20;if($stream.Read($head,0,20)-ne 20){throw 'Truncated ELF'}}finally{$stream.Dispose()}
        $arm64=$relative.StartsWith('arm64-v8a\');$class=if($arm64){2}else{1};$machine=if($arm64){183}else{40}
        if($head[0]-ne 127-or $head[1]-ne 69-or $head[2]-ne 76-or $head[3]-ne 70-or $head[4]-ne $class-or $head[5]-ne 1-or [BitConverter]::ToUInt16($head,18)-ne $machine){throw "ELF ABI mismatch: $relative"}
        [pscustomobject]@{Relative=$relative;Path=$f.FullName;Bytes=$f.Length;SHA256=Get-TaskHash $f.FullName;CreationUtc=$f.CreationTimeUtc.ToString('O')}
    }
}
function Get-TaskGradleInputs {
    $inputs=@(Get-ChildItem -LiteralPath $taskGradle -File|Where-Object{$_.Extension-in @('.gradle','.properties')})
    foreach($folder in @('shared','gradle')){if(Test-Path -LiteralPath (Join-Path $taskGradle $folder)){$inputs+=@(Get-ChildItem -LiteralPath (Join-Path $taskGradle $folder) -File -Recurse)}}
    foreach($module in @('launcher','unityLibrary')){
        $inputs+=@(Get-ChildItem -LiteralPath (Join-Path $taskGradle $module) -File)
        if(Test-Path -LiteralPath (Join-Path $taskGradle "$module\libs")){$inputs+=@(Get-ChildItem -LiteralPath (Join-Path $taskGradle "$module\libs") -File -Recurse)}
        $inputs+=@(Get-ChildItem -LiteralPath (Join-Path $taskGradle "$module\src") -File -Recurse)
    }
    foreach($f in $inputs|Sort-Object FullName -Unique){$null=Assert-TaskPhysicalPath $f.FullName $taskGradle;[pscustomobject]@{Relative=$f.FullName.Substring($taskGradle.Length+1);Bytes=$f.Length;SHA256=Get-TaskHash $f.FullName}}
}
function Get-TaskRecoveryEvidence([switch]$Record) {
    $script:taskValidatedParentDirectories=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    try {
    $context=Assert-TaskFreshFailure;$native=@(Get-TaskNativeInputs $context.NativeStart)
    if($Record){
        if(Test-Path -LiteralPath $taskEvidencePath){throw 'Fresh recovery evidence already exists; never overwrite it.'}
        $e=[pscustomobject]@{SchemaVersion=1;BuildId=$context.Manifest.buildId;RecordedUtc=[DateTimeOffset]::UtcNow.ToString('O');UnityPid=$taskExpectedPid;UnityExited=$true;ManifestSHA256=Get-TaskHash $taskManifestPath;LaunchSHA256=Get-TaskHash $taskLaunchPath;UnityLogSHA256=Get-TaskHash $context.LogPath;UnityReportSHA256=Get-TaskHash $context.ReportPath;GradleDaemonSHA256=Get-TaskHash $taskDaemonPath;FailedPackagingTasks=$context.FailedTasks;NativeInputs=$native;GradleInputs=@(Get-TaskGradleInputs);Scope='Fresh completed Unity native outputs; valid dependency-tracked object reuse allowed. Only Gradle packaging failed for disk space.'}
        $e|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $taskEvidencePath -Encoding UTF8
        return $e
    }
    if(!(Test-Path -LiteralPath $taskEvidencePath)){throw 'Record fresh post-exit evidence using cleanup_editor_cache-fresh2abi.ps1 -RecordEvidence first.'}
    $e=Get-Content -LiteralPath $taskEvidencePath -Raw -Encoding UTF8|ConvertFrom-Json
    if($e.SchemaVersion-ne 1-or $e.BuildId-ne $taskExpectedBuildId-or !$e.UnityExited-or $e.UnityPid-ne $taskExpectedPid){throw 'Wrong or incomplete recovery evidence.'}
    foreach($pair in @(@($taskManifestPath,$e.ManifestSHA256),@($taskLaunchPath,$e.LaunchSHA256),@($context.LogPath,$e.UnityLogSHA256),@($context.ReportPath,$e.UnityReportSHA256),@($taskDaemonPath,$e.GradleDaemonSHA256))){if((Get-TaskHash $pair[0])-ne $pair[1]){throw 'Fresh source/build evidence changed after recording.'}}
    if(@($e.NativeInputs).Count-ne 6){throw 'Expected six recorded native inputs.'}
    foreach($n in $native){$old=@($e.NativeInputs|Where-Object{$_.Relative-eq $n.Relative});if($old.Count-ne 1-or $old[0].SHA256-ne $n.SHA256-or $old[0].Bytes-ne $n.Bytes){throw 'Native input no longer matches fresh post-exit hash.'}}
    $inputs=@(Get-TaskGradleInputs);if($inputs.Count-ne @($e.GradleInputs).Count){throw 'Gradle source-input set changed.'}
    $map=@{};foreach($old in $e.GradleInputs){$map[$old.Relative]=$old}
    foreach($entryInput in $inputs){if(!$map.ContainsKey($entryInput.Relative)-or $map[$entryInput.Relative].SHA256-ne $entryInput.SHA256-or $map[$entryInput.Relative].Bytes-ne $entryInput.Bytes){throw "Gradle input changed: $($entryInput.Relative)"}}
    $e
    } finally { $script:taskValidatedParentDirectories=$null }
}
