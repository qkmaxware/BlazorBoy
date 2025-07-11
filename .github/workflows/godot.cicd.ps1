param (
    [string]$GodotVersion = "4.4.1",
    [string]$ProjectPath = "C:\Path\To\Your\GodotProject",
    [string]$ExportPreset = "Windows Desktop",
    [string]$OutputPath = "C:\Path\To\ExportedGame",
    [string]$OutputExeName = "ExportedGame.exe"
)

# Configuration
# eg: https://github.com/godotengine/godot/releases/download/4.4.1-stable/Godot_v4.4.1-stable_mono_win64.zip
$GodotDownloadBase = "https://github.com/godotengine/godot/releases/download/${GodotVersion}-stable"
$GodotExeName = "Godot_v${GodotVersion}-stable_mono_win64.exe"
$GodotZipName = "Godot_v${GodotVersion}-stable_mono_win64.zip"
$GodotUrl = "$GodotDownloadBase/$GodotZipName"

# eg: https://github.com/godotengine/godot/releases/download/4.4.1-stable/Godot_v4.4.1-stable_mono_export_templates.tpz
$TemplatesZip = "Godot_v${GodotVersion}-stable_mono_export_templates.tpz"
$TemplatesUrl = "$GodotDownloadBase/$TemplatesZip"

$TempDir = "$env:TEMP\GodotMonoDownload"
$GodotExtractPath = "$TempDir\Godot"
$TemplatesExtractPath = "$TempDir\Templates"

$GodotUserTemplatesPath = "$env:APPDATA\Godot\export_templates\${GodotVersion}.mono"

# Ensure directories exist
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
New-Item -ItemType Directory -Path $GodotExtractPath -Force | Out-Null
New-Item -ItemType Directory -Path $TemplatesExtractPath -Force | Out-Null
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

Write-Host "Downloading Godot Mono ($GodotVersion) from ($GodotUrl)..."
Invoke-WebRequest -Uri $GodotUrl -OutFile "$TempDir\$GodotZipName"

Write-Host "Extracting Godot to ($GodotExtractPath)..."
Expand-Archive -Path "$TempDir\$GodotZipName" -DestinationPath $GodotExtractPath -Force

$GodotExePath = Get-ChildItem -Path $GodotExtractPath -Filter "*.exe" | Select-Object -First 1

Write-Host "Downloading C# export templates from ($TemplatesUrl)..."
Invoke-WebRequest -Uri $TemplatesUrl -OutFile "$TempDir\$TemplatesZip"

Write-Host "Extracting templates to ($TemplatesExtractPath)..."
tar -xvzf "$TempDir\$TemplatesZip" -C $TemplatesExtractPath

Write-Host "Installing templates to ($GodotUserTemplatesPath)..."
Copy-Item -Path "$TemplatesExtractPath\templates" -Destination $GodotUserTemplatesPath -Recurse -Force

Write-Host "Exporting project using Godot CLI..."
& "$($GodotExePath.FullName)" --headless --verbose --path $ProjectPath --export-release "$ExportPreset" "$OutputPath\$OutputExeName"

Write-Host "Done!"