$ErrorActionPreference='Stop'
$project='C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work\PocketPit'
$scratch='C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe'
$running=Get-Process -Id 36892 -ErrorAction SilentlyContinue
if($running){$running.WaitForExit()}
[xml]$physical=Get-Content (Join-Path $scratch 'tower-test2.xml')
if([int]$physical.'test-run'.failed -ne 0){throw 'Physical campaign failed'}
$editArgs='-batchmode -runTests -testPlatform EditMode -projectPath "'+$project+'" -testResults "'+$scratch+'\edit-final.xml" -logFile "'+$scratch+'\edit-final.log"'
$task=Start-Process -FilePath $unity -ArgumentList $editArgs -WindowStyle Hidden -PassThru -Wait
if($task.ExitCode -ne 0){throw 'Edit tests failed'}
$playArgs='-batchmode -runTests -testPlatform PlayMode -testFilter "CampaignPlayModeTests;ReleasePlayTests.CaptureScreensAndCheckFonts;ReleasePlayTests.RelaxedModeDoesNotCountDown" -projectPath "'+$project+'" -testResults "'+$scratch+'\play-other-final.xml" -logFile "'+$scratch+'\play-other-final.log"'
$task=Start-Process -FilePath $unity -ArgumentList $playArgs -WindowStyle Hidden -PassThru -Wait
if($task.ExitCode -ne 0){throw 'Play tests failed'}
$buildArgs='-batchmode -quit -projectPath "'+$project+'" -executeMethod ReleasePreparation.BuildWindows -logFile "'+$scratch+'\windows-final.log"'
$task=Start-Process -FilePath $unity -ArgumentList $buildArgs -WindowStyle Hidden -PassThru -Wait
if($task.ExitCode -ne 0){throw 'Preview build failed'}
$previewArgs='-force-d3d11 -screen-width 540 -screen-height 960 -screen-fullscreen 0 --capture "'+$scratch+'\final-previews" -logFile "'+$scratch+'\preview-final.log"'
$task=Start-Process -FilePath ($project+'\Builds\Windows\PocketPit.exe') -ArgumentList $previewArgs -WindowStyle Hidden -PassThru -Wait
if($task.ExitCode -ne 0){throw 'Preview capture failed'}
Write-Output 'FINAL CHECKS AND PREVIEWS COMPLETE'
