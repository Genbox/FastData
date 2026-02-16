$Color = "DarkBlue"

Write-Host -ForegroundColor $Color "Checking g++ availability"

$gcc = Get-Command g++ -ErrorAction SilentlyContinue
if ($gcc)
{
    Write-Host -ForegroundColor $Color "g++ found: $( $gcc.Source )"
    exit 0
}

Write-Host -ForegroundColor $Color "Installing MinGW-w64 (g++) via winget"

if (-not (Get-Command winget -ErrorAction SilentlyContinue))
{
    Write-Host -ForegroundColor Red "winget not found. Install App Installer from Microsoft Store."
    exit 1
}

winget install -e --id BrechtSanders.WinLibs.POSIX.MSVCRT --accept-package-agreements --accept-source-agreements

Write-Host -ForegroundColor Yellow "Restart PowerShell and try again."
