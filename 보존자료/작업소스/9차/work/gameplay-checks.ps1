$ErrorActionPreference='Stop'
$scratch=$PSScriptRoot
$project=Join-Path $scratch 'AndroidFinal'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe'
function RunUnity([string]$argsText,[string]$tag){
 $arguments='-batchmode -projectPath "'+$project+'" '+$argsText+' -logFile "'+$scratch+'\'+$tag+'.log"'
 $proc=Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
 if($proc.ExitCode -ne 0){throw ($tag+' failed: '+$proc.ExitCode)}
 Write-Output ($tag+' COMPLETE')
}
RunUnity '-quit -executeMethod ReleasePreparation.BuildWindows' 'gameplay-windows'
$captureArgs='-force-d3d11 -screen-width 540 -screen-height 960 -screen-fullscreen 0 --capture "'+$scratch+'\gameplay-previews" -logFile "'+$scratch+'\gameplay-preview.log"'
$capture=Start-Process -FilePath "$project/Builds/Windows/PocketPit.exe" -ArgumentList $captureArgs -WindowStyle Hidden -PassThru -Wait
if($capture.ExitCode -ne 0){throw 'Capture failed'}
Write-Output 'GAMEPLAY PREVIEWS READY'
RunUnity ('-runTests -testPlatform EditMode -testResults "'+$scratch+'\gameplay-edit.xml"') 'gameplay-edit'
RunUnity ('-runTests -testPlatform PlayMode -testResults "'+$scratch+'\gameplay-play.xml"') 'gameplay-play'
Write-Output 'GAMEPLAY CHECKS COMPLETE'
