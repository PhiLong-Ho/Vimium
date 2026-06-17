<#
    Registers a Scheduled Task that auto-starts hap.exe at logon WITH highest
    privileges, so the elevated (requireAdministrator) app starts without a UAC
    prompt every time you log in.

    Run elevated. Safe to re-run (it replaces any existing task of the same name).
#>
[CmdletBinding()]
param(
    [string]$ExePath = "$env:LOCALAPPDATA\Programs\HuntAndPeck\hap.exe",
    [string]$TaskName = "HuntAndPeck (Vim with mouse)"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ExePath)) {
    throw "hap.exe not found at '$ExePath'."
}

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

Write-Host "Scheduled task '$TaskName' registered."
Write-Host "It will auto-start (elevated, no UAC prompt) at next logon."
