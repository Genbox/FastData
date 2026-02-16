$Color = "DarkBlue"

Write-Host -ForegroundColor $Color "Checking rustc availability"

$rustc = Get-Command rustc -ErrorAction SilentlyContinue
if ($rustc) {
    Write-Host -ForegroundColor $Color "rustc found: $($rustc.Source)"
    exit 0
}

Write-Host -ForegroundColor $Color "Installing Rust (rustc) via winget"

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Host -ForegroundColor Red "winget not found. Install App Installer from Microsoft Store."
    exit 1
}

winget install -e --id Rustlang.Rustup --accept-package-agreements --accept-source-agreements

$rustc = Get-Command rustc -ErrorAction SilentlyContinue
if ($rustc) {
    Write-Host -ForegroundColor $Color "rustc found: $($rustc.Source)"
    exit 0
}

Write-Host -ForegroundColor Yellow "rustc not found on PATH after installation. Restart PowerShell and try again."
