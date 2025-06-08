$Config = "Debug"
$Root = (Resolve-Path "$PSScriptRoot/..").Path
$Color = "DarkBlue"

Write-Host -BackgroundColor $Color "Building solution"
dotnet build $Root/FastData.sln -c $Config