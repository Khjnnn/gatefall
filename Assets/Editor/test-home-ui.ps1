$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskFiles=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Core" -Filter '*.cs').FullName)
$taskFiles+="$taskProject/Assets/Scripts/Presentation/HomeMenu.cs"
$taskFiles+="$taskProject/Assets/Scripts/Presentation/CampaignController.cs"
$taskFiles+="$taskProject/Assets/Scripts/Presentation/CampaignController.Shop.cs"
$taskFiles+="$taskProject/Assets/Scripts/Presentation/ShopLabels.cs"
$taskFiles+="$taskProject/Assets/Scripts/Presentation/DailyLabels.cs"
$taskFiles+="$taskProject/Assets/Scripts/Presentation/CampaignController.Daily.cs"
$taskFiles+="$taskProject/Assets/Editor/HomeFlowChecks.cs"
$taskFiles+=@('MobileChecks','HudStatusChecks','HudOcclusionChecks')|ForEach-Object{"$taskProject/Tests/$_.cs"}
Add-Type -CompilerOptions @('/langversion:7.3','/define:GATEFALL_HOME_CHECKS') -Path $taskFiles
[Gatefall.Tests.HomeChecks]::Run()
[Gatefall.GatefallGame]::RunHomeFlowChecks()
[Gatefall.GatefallGame]::RunShopFlowChecks()
[Gatefall.GatefallGame]::RunDailyFlowChecks()
