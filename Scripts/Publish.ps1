param(
    [string]$NuGetKey = $env:NUGET_KEY,
    [string]$PwshGKey = $env:PWSHG_KEY,
    [string]$GitHubToken = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest

. "$PSScriptRoot/Common.ps1"

$Config = "Release"
$Root = (Resolve-Path "$PSScriptRoot/..").Path
$Solution = "$Root/FastData.slnx"
$PublishDir = "$Root/Publish"
$ArtifactsDir = "$Root/Publish/Artifacts"
$Color = "Blue"

$CommonProperties = @("-p:PackAssemblyName=true")
$LibraryPublishProperties = $CommonProperties + @("-p:GenerateDependencyFile=false")
$CliPublishProperties = $CommonProperties + @("-p:PublishSingleFile=true", "-p:SelfContained=true", "-p:PublishTrimmed=true", "-p:TargetFrameworks=net10.0", "-p:DebugType=none", "-p:GenerateDocumentationFile=false", "-p:EnableCompressionInSingleFile=true", "-p:InvariantGlobalization=true")
$PackCommonProperties = $CommonProperties + @("-p:ContinuousIntegrationBuild=true", "-p:ValidatePackageMetadata=true")

# Prerequsites
Write-Host -ForegroundColor $Color "Installing prerequisites"
Invoke-DotNet tool update --global minver-cli

Write-Host -ForegroundColor $Color "Clean up from previous publishes"
if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Path $PublishDir | Out-Null

$version = minver;
Write-Host -ForegroundColor Cyan "Version: $version"

# Override version so minver does not calculate it per package
$env:MinVerVersionOverride = $version

# Verify the release before generating custom artifacts.
Invoke-DotNet restore $Solution --locked-mode
Invoke-DotNet build $Solution -c $Config --no-restore @PackCommonProperties
Invoke-DotNet test --solution $Solution -c $Config --no-build @CommonProperties

Write-Host -ForegroundColor $Color "Publish the dll files"
$PwshFramework = "netstandard2.0" # Version needed by PowerShell
Invoke-DotNet publish $Root/Src/FastData/FastData.csproj -c $Config -f $PwshFramework @LibraryPublishProperties -o $ArtifactsDir
Invoke-DotNet publish $Root/Src/FastData.Generator/FastData.Generator.csproj -c $Config -f $PwshFramework @LibraryPublishProperties -o $ArtifactsDir
Invoke-DotNet publish $Root/Src/FastData.Generator.CSharp/FastData.Generator.CSharp.csproj -c $Config -f $PwshFramework @LibraryPublishProperties -o $ArtifactsDir
Invoke-DotNet publish $Root/Src/FastData.Generator.CPlusPlus/FastData.Generator.CPlusPlus.csproj -c $Config -f $PwshFramework @LibraryPublishProperties -o $ArtifactsDir
Invoke-DotNet publish $Root/Src/FastData.Generator.Rust/FastData.Generator.Rust.csproj -c $Config -f $PwshFramework @LibraryPublishProperties -o $ArtifactsDir

Write-Host -ForegroundColor $Color "Pack the CLI tool as executable"
Invoke-DotNet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -r win-x64 @CliPublishProperties -o $PublishDir
Move-Item $PublishDir/Genbox.FastData.Cli.exe $PublishDir/FastData-win.exe -Force

Invoke-DotNet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -r linux-x64 @CliPublishProperties -o $PublishDir
Move-Item $PublishDir/Genbox.FastData.Cli $PublishDir/FastData-lin -Force

Invoke-DotNet publish $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config -r osx-x64 @CliPublishProperties -o $PublishDir
Move-Item $PublishDir/Genbox.FastData.Cli $PublishDir/FastData-osx -Force

Write-Host -ForegroundColor $Color "Pack the CLI tool as a dotnet tool (NuGet)"
Invoke-DotNet pack $Root/Src/FastData.Cli/FastData.Cli.csproj -c $Config --no-build @PackCommonProperties -p:PackAsTool=true -p:ToolCommandName=fastdata -p:PackageVersion=$version -o $PublishDir

Write-Host -ForegroundColor $Color "Pack the source generator"
Invoke-DotNet pack $Root/Src/FastData.SourceGenerator/FastData.SourceGenerator.csproj -c $Config --no-build @PackCommonProperties -p:PackageVersion=$version -o $PublishDir

Write-Host -ForegroundColor $Color "Pack FastData as a library"
Invoke-DotNet pack $Root/Src/FastData/FastData.csproj -c $Config --no-build @PackCommonProperties -o $PublishDir
Invoke-DotNet pack $Root/Src/FastData.Generator/FastData.Generator.csproj -c $Config --no-build @PackCommonProperties -o $PublishDir
Invoke-DotNet pack $Root/Src/FastData.Generator.Template/FastData.Generator.Template.csproj -c $Config --no-build @PackCommonProperties -o $PublishDir
Invoke-DotNet pack $Root/Src/FastData.Generator.CSharp/FastData.Generator.CSharp.csproj -c $Config --no-build @PackCommonProperties -o $PublishDir
Invoke-DotNet pack $Root/Src/FastData.Generator.CPlusPlus/FastData.Generator.CPlusPlus.csproj -c $Config --no-build @PackCommonProperties -o $PublishDir
Invoke-DotNet pack $Root/Src/FastData.Generator.Rust/FastData.Generator.Rust.csproj -c $Config --no-build @PackCommonProperties -o $PublishDir

$Packages = @(Get-ChildItem -LiteralPath $PublishDir -Filter "*.nupkg" -File)
$SymbolPackages = @(Get-ChildItem -LiteralPath $PublishDir -Filter "*.snupkg" -File)

if ($Packages.Count -eq 0) {
    throw "Expected at least one NuGet package, but found none."
}

if ($Packages.Count -ne $SymbolPackages.Count) {
    throw "Expected one symbol package per NuGet package, but found $($Packages.Count) NuGet and $($SymbolPackages.Count) symbol packages."
}

$PackageStems = @($Packages.BaseName | Sort-Object)
$SymbolPackageStems = @($SymbolPackages.BaseName | Sort-Object)

if (Compare-Object $PackageStems $SymbolPackageStems -CaseSensitive) {
    throw "NuGet and symbol package filename stems do not match."
}

if ($Packages.Where({ -not $_.Name.StartsWith("Genbox.FastData.", [StringComparison]::Ordinal) }).Count -ne 0) {
    throw "NuGet package filename does not use the expected project prefix."
}

Write-Host -ForegroundColor $Color "Pack the PowerShell variant"
$ModuleDir = "$PublishDir/Genbox.FastData"
New-Item -ItemType Directory -Path $ModuleDir | Out-Null
New-Item -ItemType Directory -Path $ModuleDir/lib | Out-Null

Write-Host -ForegroundColor $Color "Copy over the other PowerShell files"
Copy-Item $Root/Misc/PowerShell/FastData.psm1 $ModuleDir/Genbox.FastData.psm1
Copy-Item $ArtifactsDir/*.dll $ModuleDir/lib/
Copy-Item $ArtifactsDir/Templates $ModuleDir/lib/Templates -Recurse -Force

Write-Host -ForegroundColor $Color "Copy over the psd1 file, update the version number, and generate the file list"
$ManifestPath = "$ModuleDir/Genbox.FastData.psd1"
$FileList = @("Genbox.FastData.psd1") + @(Get-ChildItem -Path $ModuleDir -Recurse -File |
    ForEach-Object { [System.IO.Path]::GetRelativePath($ModuleDir, $_.FullName).Replace('/', '\') } |
    Where-Object { $_ -ne "Genbox.FastData.psd1" } |
    Sort-Object)
$FileListText = ($FileList | ForEach-Object { "                          '$($_.Replace("'", "''"))'" }) -join ",`n"
(Get-Content $Root/Misc/PowerShell/FastData.psd1 -Raw).Replace("TODO-VERSION", $version).Replace("                          'TODO-FILELIST'", $FileListText) | Set-Content $ManifestPath

# We don't want to publish versions with tags like "alpha", "beta", etc.
if ($version -notlike "*-*") {
    if ($PwshGKey) {
        Write-Host -ForegroundColor $Color "Publish PowerShell to PowerShell Gallery"
        Publish-Module -Path "$PublishDir/Genbox.FastData/" -NuGetApiKey $PwshGKey
    }
    else {
        Write-Host -ForegroundColor Yellow "Skipping PowerShell publish: PWSHG_KEY not set."
    }

    if ($NuGetKey) {
        Write-Host -ForegroundColor $Color "Publish dotnet tool to NuGet"
        foreach ($Package in $Packages) {
            # NuGet automatically pushes the matching symbol package.
            Invoke-DotNet nuget push --skip-duplicate $Package.FullName --api-key $NuGetKey --source https://api.nuget.org/v3/index.json
        }
    }
    else {
        Write-Host -ForegroundColor Yellow "Skipping NuGet push: NUGET_KEY not set."
    }
}
else {
    Write-Host -ForegroundColor Yellow "Skipping publish due to alpha build"
}