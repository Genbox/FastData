$Config = "Debug"
$Root = "$PSScriptRoot/.."
$Color = "DarkBlue"

Write-Host -BackgroundColor $Color "Building solution"
dotnet build $Root/FastData.sln -c $Config