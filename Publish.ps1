$Config = "Release"
$Framework = "net9.0"
$PublishDir = "$PSScriptRoot/Publish"
$ArtifactsDir = "$PSScriptRoot/Publish/Artifacts"

# Clean up from previous publishes
Remove-Item -Path $PublishDir/* -Recurse -Force

# Publish the dll files
dotnet publish $PSScriptRoot/Src/FastData/FastData.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $PSScriptRoot/Src/FastData.Generator.CSharp/FastData.Generator.CSharp.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $PSScriptRoot/Src/FastData.Generator.CPlusPlus/FastData.Generator.CPlusPlus.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $PSScriptRoot/Src/FastData.Generator.Rust/FastData.Generator.Rust.csproj -c $Config -f $Framework -p:GenerateDependencyFile=false -o $ArtifactsDir

# Pack the PowerShell variant
New-Item -ItemType Directory -Path $PublishDir/PowerShell | Out-Null
New-Item -ItemType Directory -Path $PublishDir/PowerShell/lib | Out-Null

# Get the version number of the generator dll file
$ver = [System.Reflection.AssemblyName]::GetAssemblyName("$ArtifactsDir\Genbox.FastData.dll").Version
$semver = "$($ver.Major).$($ver.Minor).$($ver.Build)"

# Copy over the psd1 file, and update the version number in the process
(Get-Content $PSScriptRoot/Misc/PowerShell/FastData.psd1) -replace "TODO-VERSION", "$semver" | Set-Content $PublishDir/PowerShell/FastData.psd1

# Copy over the other PowerShell files
Copy-Item $PSScriptRoot/Misc/PowerShell/FastData.psm1 $PublishDir/PowerShell/FastData.psm1
Copy-Item $ArtifactsDir/*.dll $PublishDir/PowerShell/lib/

# Pack the CLI tool as executable
dotnet publish $PSScriptRoot/Src/FastData.Cli/FastData.Cli.csproj -c $Config -f $Framework -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:TargetFrameworks=$Framework -p:DebugType=none -p:GenerateDocumentationFile=false -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -o $PublishDir

# Pack the CLI tool as a dotnet tool (nuget)
dotnet pack $PSScriptRoot/Src/FastData.Cli/FastData.Cli.csproj -c $Config -p:PackAsTool=true -p:ToolCommandName=fastdata -p:PackageVersion=$semver -o $PublishDir

$PublishDir = "$PSScriptRoot/Publish"

# Publish PowerShell module to PowerShellGallery
Publish-Module -Path "$PublishDir/PowerShell/" -NuGetApiKey $env:NUGET_KEY

# Publish dotnet tool to NuGet
dotnet nuget push $PublishDir/*.nupkg --api-key $env:NUGET_KEY