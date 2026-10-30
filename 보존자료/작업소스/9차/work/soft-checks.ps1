$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$project=Join-Path $taskRoot 'AndroidFinal'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe'
$building=Get-Process -Id 9376 -ErrorAction SilentlyContinue
if($building){$building.WaitForExit()}
if(!(Select-String -LiteralPath "$taskRoot/soft-windows.log" -SimpleMatch 'Build Finished, Result: Success.')){throw 'Preview build failed'}
$captureArgs='-force-d3d11 -screen-width 540 -screen-height 960 -screen-fullscreen 0 --capture "'+$taskRoot+'\soft-previews" -logFile "'+$taskRoot+'\soft-preview.log"'
$capture=Start-Process -FilePath "$project/Builds/Windows/PocketPit.exe" -ArgumentList $captureArgs -WindowStyle Hidden -PassThru -Wait
if($capture.ExitCode -ne 0){throw 'Capture failed'}
Write-Output 'SOFT PREVIEWS READY'
$testArgs='-batchmode -runTests -testPlatform PlayMode -testFilter "BombPlayTests;CampaignPlayModeTests;ReleasePlayTests.RelaxedModeDoesNotCountDown" -projectPath "'+$project+'" -testResults "'+$taskRoot+'\soft-play.xml" -logFile "'+$taskRoot+'\soft-play.log"'
$tests=Start-Process -FilePath $unity -ArgumentList $testArgs -WindowStyle Hidden -PassThru -Wait
if($tests.ExitCode -ne 0){throw 'Play tests failed'}
Write-Output 'SOFT PLAY TESTS PASSED'
