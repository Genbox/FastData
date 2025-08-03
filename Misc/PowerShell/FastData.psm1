function Invoke-FastData
{
    param (
        [Parameter(Mandatory = $true)][ValidateSet("csharp", "cpp", "rust")] [string]$Command,
        [string]$OutputFile,
        [ValidateSet("char", "double", "int8", "int16", "int32", "int64", "int8", "single", "string", "uint8", "uint16", "uint32", "uint64" )]
        [string]$DataType = "string",
        [ValidateSet("Auto", "Array", "BinarySearch", "Conditional", "HashTable")]
        [string]$StructureType = "Auto",
        [Parameter(Mandatory = $true)] [string]$InputFile,
        [string]$Namespace,
        [string]$ClassName = "MyData"
    )

    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.CSharp.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.CPlusPlus.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.Rust.dll" -ErrorAction Stop

    function Get-TypeFunc
    {
        param ($DataType)
        switch ($DataType)
        {
            "string" { return { [string]$_ } }
            "char" { return { [char]$_ } }
            "int8" { return { [sbyte]::Parse($_) } }
            "uint8" { return { [byte]::Parse($_) } }
            "int16" { return { [short]::Parse($_) } }
            "uint16" { return { [ushort]::Parse($_) } }
            "int32" { return { [int]::Parse($_) } }
            "uint32" { return { [uint]::Parse($_) } }
            "int64" { return { [long]::Parse($_) } }
            "uint64" { return { [ulong]::Parse($_) } }
            "single" { return { [float]::Parse($_) } }
            "double" { return { [double]::Parse($_) } }
            default { throw "Unsupported data type: $DataType" }
        }
    }

    $typeFunc = Get-TypeFunc $DataType
    $data = Get-Content -Path $InputFile | ForEach-Object { & $typeFunc $_ }
    $config = New-Object Genbox.FastData.Configs.FastDataConfig([Genbox.FastData.Enums.StructureType]::$StructureType)

    if ($Command -eq "csharp")
    {
        $cfg = New-Object Genbox.FastData.Generator.CSharp.CSharpCodeGeneratorConfig($ClassName)
        $cfg.Namespace = $Namespace
        $generator = [Genbox.FastData.Generator.CSharp.CSharpCodeGenerator]::Create($cfg)
    }
    elseif ($Command -eq "cpp")
    {
        $cfg = New-Object Genbox.FastData.Generator.CPlusPlus.CPlusPlusCodeGeneratorConfig($ClassName)
        $generator = [Genbox.FastData.Generator.CPlusPlus.CPlusPlusCodeGenerator]::Create($cfg)

    }
    elseif ($Command -eq "rust")
    {
        $cfg = New-Object Genbox.FastData.Generator.Rust.RustCodeGeneratorConfig($ClassName)
        $generator = [Genbox.FastData.Generator.Rust.RustCodeGenerator]::Create($cfg)
    }

    $source = [Genbox.FastData.FastDataGenerator]::Generate($data, $config, $generator)

    if (-not $ok)
    {
        throw "Unable to generate code"
    }

    if ($OutputFile)
    {
        $source.Value | Out-File -Encoding UTF8 -FilePath $OutputFile
    }
    else
    {
        Write-Output $source
    }
}

Export-ModuleMember -Function Invoke-FastData
