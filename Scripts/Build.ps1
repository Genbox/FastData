$Config = "Debug"
$Root = (Resolve-Path "$PSScriptRoot/..").Path
$Color = "DarkBlue"

Write-Host -ForegroundColor $Color "Building solution"
dotnet build $Root/FastData.slnx -c $Config

# DPR-059: CI build workflow invokes this script, so run tests here too.
dotnet test $Root/FastData.slnx -c $Config --no-build