$Config = "Release"
$Framework = "net9.0"
$Root = "$PSScriptRoot/../"
$PublishDir = "$Root/Publish"
$ArtifactsDir = "$Root/Publish/Artifacts"
$Color = "DarkBlue"

Write-Host -BackgroundColor $Color "Clean up from previous publishes"
Remove-Item -Path $PublishDir/* -Recurse -Force -ErrorAction Ignore | Out-Null

Write-Host -BackgroundColor $Color "Publish the dll files"
dotnet publish $Root/Src/FastData/FastData.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator.CSharp/FastData.Generator.CSharp.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator.CPlusPlus/FastData.Generator.CPlusPlus.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator.Rust/FastData.Generator.Rust.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir

Write-Host -BackgroundColor $Color "Pack the PowerShell variant"
New-Item -ItemType Directory -Path $PublishDir/PowerShell | Out-Null
New-Item -ItemType Directory -Path $PublishDir/PowerShell/lib | Out-Null

Write-Host -BackgroundColor $Color "Get the version number of the generator dll file"
$ver = [System.Reflection.AssemblyName]::GetAssemblyName("$ArtifactsDir/Genbox.FastData.dll").Version
$semver = "$($ver.Major).$($ver.Minor).$($ver.Build)"

Write-Host -BackgroundColor $Color "Version: $semver"

Write-Host -BackgroundColor $Color "Copy over the psd1 file, and update the version number in the process"
(Get-Content $Root/Misc/PowerShell/FastData.psd1) -replace "TODO-VERSION", "$semver" | Set-Content $PublishDir/PowerShell/FastData.psd1

Write-Host -BackgroundColor $Color "Copy over the other PowerShell files"
Copy-Item $Root/Misc/PowerShell/FastData.psm1 $PublishDir/PowerShell/FastData.psm1
Copy-Item $ArtifactsDir/*.dll $PublishDir/PowerShell/lib/

Write-Host -BackgroundColor $Color "Pack the CLI tool as executable"
dotnet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -f $Framework -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:TargetFrameworks=$Framework -p:DebugType=none -p:GenerateDocumentationFile=false -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -o $PublishDir

Write-Host -BackgroundColor $Color "Pack the CLI tool as a dotnet tool (NuGet)"
dotnet pack $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -p:PackAsTool=true -p:ToolCommandName=fastdata -p:PackageVersion=$semver -o $PublishDir

Write-Host -BackgroundColor $Color "Pack the source generator"
dotnet pack $Root/Src/FastData.SourceGenerator/FastData.SourceGenerator.csproj -c $Config -p:PackageVersion=$semver -o $PublishDir

#Write-Host -BackgroundColor $Color "Publish PowerShell module to PowerShellGallery"
#Publish-Module -Path "$PublishDir/PowerShell/" -NuGetApiKey $env:PWSHG_KEY

#Write-Host -BackgroundColor $Color "Publish dotnet tool to NuGet"
#dotnet nuget push $PublishDir/*.nupkg --api-key $env:NUGET_KEY