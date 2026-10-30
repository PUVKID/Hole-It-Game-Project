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
RunUnity '-quit -executeMethod ReleasePreparation.BuildWindows' 'soft-windows2'
$captureArgs='-force-d3d11 -screen-width 540 -screen-height 960 -screen-fullscreen 0 --capture "'+$scratch+'\soft-final-previews" -logFile "'+$scratch+'\soft-final-preview.log"'
$capture=Start-Process -FilePath "$project/Builds/Windows/PocketPit.exe" -ArgumentList $captureArgs -WindowStyle Hidden -PassThru -Wait
if($capture.ExitCode -ne 0){throw 'Capture failed'}
Write-Output 'FINAL PREVIEWS READY'
RunUnity ('-runTests -testPlatform EditMode -testResults "'+$scratch+'\soft-edit-final.xml"') 'soft-edit-final'
RunUnity ('-runTests -testPlatform PlayMode -testResults "'+$scratch+'\soft-play-final.xml"') 'soft-play-final'
RunUnity '-diag-debug-shader-compiler -buildTarget Android -executeMethod AndroidRelease.Build' 'soft-android-final'
Write-Output 'SOFT EDITION ALL CHECKS AND BUILDS COMPLETE'
