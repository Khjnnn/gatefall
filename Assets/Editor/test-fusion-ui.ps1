param([switch]$Compile)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if($Compile) {
    $taskManaged='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/Managed'
    $taskReferences=@((Get-ChildItem -LiteralPath "$PSHOME/ref" -Filter '*.dll').FullName)
    $taskReferences+=@((Get-ChildItem -LiteralPath "$taskManaged/UnityEngine" -Filter '*.dll').FullName)
    $taskReferences+=@((Get-ChildItem -LiteralPath "$taskManaged" -Filter 'UnityEditor.*Module.dll').FullName)
    $taskReferences+="$taskProject/Library/ScriptAssemblies/UnityEngine.UI.dll"
    $taskFiles=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Core" -Filter '*.cs').FullName)
    $taskFiles+=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Presentation" -Filter '*.cs').FullName)
    $taskFiles+="$taskProject/Assets/Editor/ChapterTwoAudit.cs"
    Add-Type -CompilerOptions @('/langversion:9.0','/nowarn:0618') -ReferencedAssemblies $taskReferences -Path $taskFiles
    'PASS: Core, all Presentation and recipe/fit width audit compile against Unity 6000.6.7f1 references; no Unity process invoked.'
    return
}
$taskFiles=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Core" -Filter '*.cs').FullName)
$taskFiles+="$taskProject/Assets/Scripts/Presentation/FusionPreview.cs"
$taskFiles+="$taskProject/Assets/Editor/FusionPreviewChecks.cs"
$taskFiles+="$taskProject/Tests/HudOcclusionChecks.cs"
Add-Type -CompilerOptions '/langversion:7.3' -Path $taskFiles
[Gatefall.Tests.FusionPreviewChecks]::Run()
"PASS: $([Gatefall.Tests.HudOcclusionChecks]::FusionGeometry()) fusion fit-label geometry/occlusion cases across twelve screens."
& (Join-Path $PSHOME 'pwsh.exe') -NoProfile -File $PSCommandPath -Compile
if($LASTEXITCODE -ne 0) { throw 'Fusion presentation/reference compile failed.' }
