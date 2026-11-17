$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskManaged='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/Managed'
$taskReferences=@((Get-ChildItem -LiteralPath "$PSHOME/ref" -Filter '*.dll').FullName)
$taskReferences+=@((Get-ChildItem -LiteralPath "$taskManaged/UnityEngine" -Filter '*.dll').FullName)
$taskReferences+=@((Get-ChildItem -LiteralPath "$taskManaged" -Filter 'UnityEditor.*Module.dll').FullName)
$taskReferences+="$taskProject/Library/ScriptAssemblies/UnityEngine.UI.dll"
$taskFiles=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Core" -Filter '*.cs').FullName)
$taskFiles+=@((Get-ChildItem -LiteralPath "$taskProject/Assets/Scripts/Presentation" -Filter '*.cs').FullName)
$taskFiles+="$taskProject/Assets/Editor/HomeUiAudit.cs"
Add-Type -CompilerOptions @('/langversion:9.0','/nowarn:0618') -ReferencedAssemblies $taskReferences -Path $taskFiles
'PASS: Core, all Presentation and HomeUiAudit compile against Unity 6000.6.7f1 reference assemblies; no Unity process or lifecycle invoked.'
