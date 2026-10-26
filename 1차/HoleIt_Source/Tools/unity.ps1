param(
    [ValidateSet("validate","editmode","playmode","android","rebuild")][string]$Action="validate",
    [string]$Unity=$env:UNITY_EDITOR
)
$ErrorActionPreference="Stop"
$Root=(Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not $Unity -or -not (Test-Path -LiteralPath $Unity -PathType Leaf)) {
    Write-Error "BLOCKED: pass -Unity with an installed Unity 6000.3.6f1 Unity.exe path."
    exit 69
}
$Results=Join-Path $Root "TestResults"
New-Item -ItemType Directory -Path $Results -Force | Out-Null
$UnityArgs=@("-batchmode","-projectPath",$Root,"-logFile",(Join-Path $Results "$Action.log"))
switch($Action) {
    "validate" { $UnityArgs+=@("-nographics","-executeMethod","HoleItProjectValidation.ValidateCli") }
    "editmode" { $UnityArgs+=@("-runTests","-testPlatform","EditMode","-testFilter","CampaignEditModeTests","-testResults",(Join-Path $Results "EditMode.xml")) }
    "playmode" { $UnityArgs+=@("-runTests","-testPlatform","PlayMode","-testFilter","CampaignPlayModeTests","-testResults",(Join-Path $Results "PlayMode.xml")) }
    "android" { $UnityArgs+=@("-nographics","-buildTarget","Android","-executeMethod","HoleItBuildScript.BuildAndroid") }
    "rebuild" { $UnityArgs+=@("-executeMethod","HoleItBuilder.BuildAllCli") }
}
$Quoted=($UnityArgs | ForEach-Object { '"' + $_.Replace('"','\"') + '"' }) -join ' '
$Process=Start-Process -FilePath $Unity -ArgumentList $Quoted -Wait -PassThru
exit $Process.ExitCode
