param([string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskAndroid = Join-Path (Split-Path -Parent $EditorPath) 'Data\PlaybackEngines\AndroidPlayer'
$taskBuildTools = Join-Path $taskAndroid 'SDK\build-tools\36.0.0'
$taskApk = Join-Path $taskProject 'Builds\Android\Gatefall.apk'
$taskSettings = Get-Content -LiteralPath (Join-Path $taskProject 'ProjectSettings\ProjectSettings.asset') -Raw
$taskVersion = [regex]::Match($taskSettings, '(?m)^  bundleVersion: (\S+)').Groups[1].Value
$taskVersionCode = [regex]::Match($taskSettings, '(?m)^  AndroidBundleVersionCode: (\d+)').Groups[1].Value
if (-not $taskVersion -or -not $taskVersionCode) { throw 'Project version settings are missing' }
& (Join-Path $taskAndroid 'OpenJDK\bin\java.exe') -jar (Join-Path $taskBuildTools 'lib\apksigner.jar') verify --verbose $taskApk
if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed' }
& (Join-Path $taskBuildTools 'zipalign.exe') -c -P 16 4 $taskApk
if ($LASTEXITCODE -ne 0) { throw 'APK alignment verification failed' }
$taskBadging = & (Join-Path $taskBuildTools 'aapt.exe') dump badging $taskApk
if ($LASTEXITCODE -ne 0 -or -not ($taskBadging -match ("versionName='" + [regex]::Escape($taskVersion) + "'")) -or
    -not ($taskBadging -match ("versionCode='" + $taskVersionCode + "'")) -or
    -not ($taskBadging -match "name='com.gatefall.prototype'") -or -not ($taskBadging -match "native-code: 'arm64-v8a'")) { throw 'APK identity/version/architecture verification failed' }
$taskHash = (Get-FileHash -LiteralPath $taskApk -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath ($taskApk+'.sha256') -Value ($taskHash+'  Gatefall.apk') -Encoding ascii
$taskReport = [ordered]@{ version=$taskVersion; versionCode=[int]$taskVersionCode; created=(Get-Date).ToString('o'); bytes=(Get-Item -LiteralPath $taskApk).Length; sha256=$taskHash; signature='verified'; alignment16KB='verified'; badging=@($taskBadging | Where-Object { $_ -match "^(package:|sdkVersion:|targetSdkVersion:|native-code:)" }); physicalDeviceTested=$false }
$taskReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath TestResults/android-apk-results.json -Encoding utf8

