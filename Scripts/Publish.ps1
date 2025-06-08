$Config = "Release"
$Root = (Resolve-Path "$PSScriptRoot/..").Path
$PublishDir = "$Root/Publish"
$ArtifactsDir = "$Root/Publish/Artifacts"
$Color = "Blue"

# Prerequsites
Write-Host -ForegroundColor $Color "Installing prerequisites"
dotnet tool install --global minver-cli

Write-Host -ForegroundColor $Color "Clean up from previous publishes"
Remove-Item -Path $PublishDir/* -Recurse -Force -ErrorAction Ignore | Out-Null

$version = minver;
Write-Host -ForegroundColor Cyan "Version: $version"

# Override version so minver does not calculate it per package
$env:MinVerVersionOverride = $version

Write-Host -ForegroundColor $Color "Publish the dll files"
$PwshFramework = "netstandard2.0" # Version needed by PowerShell
dotnet publish $Root/Src/FastData/FastData.csproj -c $Config -f $PwshFramework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator/FastData.Generator.csproj -c $Config -f $PwshFramework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator.CSharp/FastData.Generator.CSharp.csproj -c $Config -f $PwshFramework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator.CPlusPlus/FastData.Generator.CPlusPlus.csproj -c $Config -f $PwshFramework -p:GenerateDependencyFile=false -o $ArtifactsDir
dotnet publish $Root/Src/FastData.Generator.Rust/FastData.Generator.Rust.csproj -c $Config -f $PwshFramework -p:GenerateDependencyFile=false -o $ArtifactsDir

Write-Host -ForegroundColor $Color "Pack the CLI tool as executable"
dotnet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:TargetFrameworks="net9.0" -p:DebugType=none -p:GenerateDocumentationFile=false -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -o $PublishDir
Move-Item $PublishDir/Genbox.FastData.Cli.exe $PublishDir/FastData-win.exe -Force

dotnet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:TargetFrameworks="net9.0" -p:DebugType=none -p:GenerateDocumentationFile=false -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -o $PublishDir
Move-Item $PublishDir/Genbox.FastData.Cli $PublishDir/FastData-lin -Force

dotnet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -r osx-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:TargetFrameworks="net9.0" -p:DebugType=none -p:GenerateDocumentationFile=false -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -o $PublishDir
Move-Item $PublishDir/Genbox.FastData.Cli $PublishDir/FastData-osx -Force

Write-Host -ForegroundColor $Color "Pack the CLI tool as a dotnet tool (NuGet)"
dotnet pack $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -p:ContinuousIntegrationBuild=true -p:PackAsTool=true -p:ToolCommandName=fastdata -p:PackageVersion=$semver -o $PublishDir

Write-Host -ForegroundColor $Color "Pack the source generator"
dotnet pack $Root/Src/FastData.SourceGenerator/FastData.SourceGenerator.csproj -c $Config -p:ContinuousIntegrationBuild=true -p:PackageVersion=$semver -o $PublishDir

Write-Host -ForegroundColor $Color "Pack FastData as a library"
dotnet pack $Root/Src/FastData/FastData.csproj -c $Config -p:ContinuousIntegrationBuild=true -o $PublishDir
dotnet pack $Root/Src/FastData.Generator/FastData.Generator.csproj -c $Config -p:ContinuousIntegrationBuild=true -o $PublishDir
dotnet pack $Root/Src/FastData.Generator.CSharp/FastData.Generator.CSharp.csproj -c $Config -p:ContinuousIntegrationBuild=true -o $PublishDir
dotnet pack $Root/Src/FastData.Generator.CPlusPlus/FastData.Generator.CPlusPlus.csproj -c $Config -p:ContinuousIntegrationBuild=true -o $PublishDir
dotnet pack $Root/Src/FastData.Generator.Rust/FastData.Generator.Rust.csproj -c $Config -p:ContinuousIntegrationBuild=true -o $PublishDir

Write-Host -ForegroundColor $Color "Pack the PowerShell variant"
New-Item -ItemType Directory -Path $PublishDir/Genbox.FastData | Out-Null
New-Item -ItemType Directory -Path $PublishDir/Genbox.FastData/lib | Out-Null

Write-Host -ForegroundColor $Color "Copy over the psd1 file, and update the version number in the process"
(Get-Content $Root/Misc/PowerShell/FastData.psd1) -replace "TODO-VERSION", "$version" | Set-Content $PublishDir/Genbox.FastData/Genbox.FastData.psd1

Write-Host -ForegroundColor $Color "Copy over the other PowerShell files"
Copy-Item $Root/Misc/PowerShell/FastData.psm1 $PublishDir/Genbox.FastData/Genbox.FastData.psm1
Copy-Item $ArtifactsDir/*.dll $PublishDir/Genbox.FastData/lib/

# We don't want to publish versions with tags like "alpha", "beta", etc.
if ($version -notlike "*-*") {
    if ($env:PWSHG_KEY) {
        Write-Host -ForegroundColor $Color "Publish PowerShell to PowerShell Gallery"
        Publish-Module -Path "$PublishDir/Genbox.FastData/" -NuGetApiKey $env:PWSHG_KEY
    }
    else {
        Write-Host -ForegroundColor Yellow "Skipping PowerShell publish: PWSHG_KEY not set."
    }

    if ($env:NUGET_KEY) {
        Write-Host -ForegroundColor $Color "Publish dotnet tool to NuGet"
        Get-ChildItem -Path "$PublishDir/*.nupkg" | ForEach-Object {
            dotnet nuget push --skip-duplicate $_.FullName --api-key $env:NUGET_KEY --source https://api.nuget.org/v3/index.json
        }
    }
    else {
        Write-Host -ForegroundColor Yellow "Skipping NuGet push: NUGET_KEY not set."
    }
}
else {
    Write-Host -ForegroundColor Yellow "Skipping publish due to alpha build"
}