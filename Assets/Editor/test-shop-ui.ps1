$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskFiles=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Core" -Filter '*.cs').FullName)
$taskFiles+=@('HomeMenu','ShopLabels','DailyLabels','CampaignController','CampaignController.Shop','CampaignController.Daily')|ForEach-Object{"$taskProject/Assets/Scripts/Presentation/$_.cs"}
$taskFiles+="$taskProject/Assets/Editor/HomeFlowChecks.cs"
$taskFiles+=@('MobileChecks','HudStatusChecks','HudOcclusionChecks')|ForEach-Object{"$taskProject/Tests/$_.cs"}
Add-Type -CompilerOptions @('/langversion:7.3','/define:GATEFALL_HOME_CHECKS') -Path $taskFiles
[Gatefall.Tests.ShopChecks]::Run()
[Gatefall.GatefallGame]::RunShopFlowChecks()
