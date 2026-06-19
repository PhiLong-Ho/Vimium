<#
    Installs Vimium: registers a Scheduled Task for elevated auto-start at logon
    and creates a Start menu shortcut so you can pin the app or launch it manually.

    Run elevated. Safe to re-run (replaces any existing task/shortcut of the same name).
#>
[CmdletBinding()]
param(
    [string]$ExePath = "$env:LOCALAPPDATA\Programs\Vimium\Vimium.exe",
    [string]$TaskName = "Vimium"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ExePath)) {
    throw "Vimium.exe not found at '$ExePath'. Build with: dotnet publish src\HuntAndPeck\HuntAndPeck.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true"
}

# -- Stage 1: Scheduled Task (elevated auto-start, no UAC prompt) --
Write-Host "Registering scheduled task '$TaskName'..."

$action = New-ScheduledTaskAction -Execute $ExePath
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal `
    -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) `
    -LogonType Interactive `
    -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit ([TimeSpan]::Zero)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Force | Out-Null

Write-Host "  -> Scheduled task '$TaskName' registered (auto-starts elevated at logon)."

# -- Stage 2: Start Menu shortcut (for pinning & manual launch) --
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuDir "Vimium.lnk"

Write-Host "Creating Start menu shortcut..."

$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $ExePath
$shortcut.WorkingDirectory = Split-Path $ExePath -Parent
$shortcut.Description = "Vimium - keyboard-driven UI navigation for Windows"
$shortcut.Save()

Write-Host "  -> Start menu shortcut created at '$shortcutPath'."
Write-Host ""
Write-Host "Done! Vimium will auto-start at next logon."
Write-Host "To pin to Start: press Win, type 'Vimium', right-click -> Pin to Start."
