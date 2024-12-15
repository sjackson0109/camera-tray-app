# Variables
$sourcePath = ".\bin\Release\net8.0-windows\win-x64\publish"
$destinationPath = "C:\Program Files\Simon Jackson\CameraTray"
$shortcutPath = "$([Environment]::GetFolderPath('CommonDesktopDirectory'))\CameraTrayApp.lnk"
$startupShortcutPath = "$([Environment]::GetFolderPath('Startup'))\CameraTrayApp.lnk"

# Ensure the source path exists
if (-Not (Test-Path $sourcePath)) {
    Write-Host "Source path not found: $sourcePath" -ForegroundColor Red
    exit 1
}
 
# Ensure the destination folder exists
if (-Not (Test-Path $destinationPath)) {
    Write-Host "Creating destination folder: $destinationPath"
    New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null
}

# Copy files from source to destination
Write-Host "Copying files to $destinationPath"
Copy-Item -Path "$sourcePath\*" -Destination $destinationPath -Recurse -Force

# Create a desktop shortcut
Write-Host "Creating desktop shortcut: $shortcutPath"
$WScriptShell = New-Object -ComObject WScript.Shell
$shortcut = $WScriptShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "$destinationPath\CameraTrayApp.exe"
$shortcut.WorkingDirectory = $destinationPath
$shortcut.WindowStyle = 1
$shortcut.IconLocation = "$destinationPath\CameraTrayApp.exe"
$shortcut.Save()

# Create a startup shortcut
Write-Host "Creating startup shortcut: $startupShortcutPath"
$startupShortcut = $WScriptShell.CreateShortcut($startupShortcutPath)
$startupShortcut.TargetPath = "$destinationPath\CameraTrayApp.exe"
$startupShortcut.WorkingDirectory = $destinationPath
$startupShortcut.WindowStyle = 1
$startupShortcut.IconLocation = "$destinationPath\CameraTrayApp.exe"
$startupShortcut.Save()

Write-Host "Deployment completed successfully!" -ForegroundColor Green
