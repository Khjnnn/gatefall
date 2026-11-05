# Source/structure validation only. Unity API resolution requires an installed Editor.
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName)
$taskOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp9)
$taskSourceFiles = Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets'),(Join-Path $taskProject 'Tests') -Filter '*.cs' -Recurse
foreach ($taskSource in $taskSourceFiles) {
    $taskText = Get-Content -LiteralPath $taskSource.FullName -Raw -Encoding utf8
    $taskTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($taskText, $taskOptions)
    $taskErrors = @($taskTree.GetDiagnostics() | Where-Object { $_.Severity -eq 'Error' })
    if ($taskErrors.Count -gt 0) { throw "$($taskSource.Name): $($taskErrors -join [Environment]::NewLine)" }
}
$taskAssets = Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets') -Recurse | Where-Object { $_.Extension -ne '.meta' }
$taskGuids = @{}
foreach ($taskAsset in $taskAssets) {
    $taskMeta = $taskAsset.FullName + '.meta'
    if (-not (Test-Path -LiteralPath $taskMeta)) { throw "Missing meta: $taskMeta" }
    $taskMetaText = Get-Content -LiteralPath $taskMeta -Raw
    $taskMatch = [regex]::Match($taskMetaText, '(?m)^guid: ([a-f0-9]{32})\s*$')
    if (-not $taskMatch.Success) { throw "Invalid asset GUID: $taskMeta" }
    $taskGuid = $taskMatch.Groups[1].Value
    if ($taskGuids.ContainsKey($taskGuid)) { throw "Duplicate asset GUID: $taskGuid" }
    $taskGuids[$taskGuid] = $taskMeta
}
$taskScene = Get-Content -LiteralPath (Join-Path $taskProject 'Assets\Scenes\Gatefall.unity') -Raw
foreach ($taskMatch in [regex]::Matches($taskScene, 'guid: ([a-f0-9]{32})')) {
    if (-not $taskGuids.ContainsKey($taskMatch.Groups[1].Value)) { throw 'Scene has an unresolved asset reference.' }
}
$taskBalance = [Gatefall.Core.GameBalance]::new()
foreach ($taskField in $taskBalance.GetType().GetFields()) {
    $taskMatch = [regex]::Match($taskScene, '(?m)^    ' + $taskField.Name + ': ([^\r\n]+)')
    if (-not $taskMatch.Success) { throw "Missing scene balance field $($taskField.Name)" }
    $taskExpected = [Convert]::ToDouble($taskField.GetValue($taskBalance), [Globalization.CultureInfo]::InvariantCulture)
    $taskActual = [double]::Parse($taskMatch.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
    if ([Math]::Abs($taskExpected - $taskActual) -gt 0.00001) { throw "Scene differs from tested defaults: $($taskField.Name)" }
}
$taskManifest = Get-Content -LiteralPath (Join-Path $taskProject 'Packages\manifest.json') -Raw | ConvertFrom-Json
if (-not $taskManifest.dependencies.'com.unity.modules.imgui') { throw 'IMGUI module missing.' }
$taskFont = [IO.File]::ReadAllBytes((Join-Path $taskProject 'Assets\Resources\Fonts\NanumGothic-Regular.ttf'))
if ($taskFont.Length -lt 1000000 -or $taskFont[0] -ne 0 -or $taskFont[1] -ne 1) { throw 'Font is not a valid expected TTF payload.' }
$taskResultsDir = Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Path $taskResultsDir -Force | Out-Null
$taskReport = "PASS: $($taskSourceFiles.Count) C# files parsed under C# 9; $($taskAssets.Count) asset metadata entries have unique GUIDs; scene references resolve; serialized balance matches tested defaults; package manifest and bundled TTF checked.`nScope: this command checks source/structure only. See runtime-verification.md for separately performed Unity/build/preview verification."
$taskReport | Set-Content -LiteralPath (Join-Path $taskResultsDir 'source-results.txt') -Encoding utf8
Write-Output $taskReport
