function Invoke-FastData
{
    param (
        [Parameter(Mandatory = $true)][ValidateSet("csharp", "cpp", "rust")] [string]$Command,
        [string]$OutputFile,
        [ValidateSet("char", "double", "int8", "int16", "int32", "int64", "single", "string", "uint8", "uint16", "uint32", "uint64" )]
        [string]$DataType = "string",
        [ValidateSet("Auto", "Array", "BinarySearch", "Conditional", "HashTable")]
        [string]$StructureType = "Auto",
        [Parameter(Mandatory = $true)] [string]$InputFile,
        [string]$Namespace,
        [string]$ClassName = "FastData"
    )

    $libDir = Join-Path $PSScriptRoot "lib"
    Get-ChildItem -Path $libDir -Filter "*.dll" | ForEach-Object {
        [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null
    }

    function Get-DataTypeInfo
    {
        param ([string]$Name)

        switch ($Name)
        {
            "string" { return [string], { param ($value) [string]$value } }
            "char" { return [char], { param ($value) if ($value.Length -ne 1) { throw "Invalid char value: '$value'" } [char]$value[0] } }
            "int8" { return [sbyte], { param ($value) [sbyte]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "uint8" { return [byte], { param ($value) [byte]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "int16" { return [short], { param ($value) [short]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "uint16" { return [ushort], { param ($value) [ushort]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "int32" { return [int], { param ($value) [int]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "uint32" { return [uint], { param ($value) [uint]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "int64" { return [long], { param ($value) [long]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "uint64" { return [ulong], { param ($value) [ulong]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "single" { return [float], { param ($value) [float]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            "double" { return [double], { param ($value) [double]::Parse($value, [Globalization.CultureInfo]::InvariantCulture) } }
            default { throw "Unsupported data type: $Name" }
        }
    }

    function Get-StructureTypeOverride
    {
        param ([string]$Name)

        switch ($Name)
        {
            "Auto" { return $null }
            "Array" { return [Type]::GetType("Genbox.FastData.Internal.Structures.ArrayStructure``2, Genbox.FastData", $true) }
            "BinarySearch" { return [Type]::GetType("Genbox.FastData.Internal.Structures.BinarySearchStructure``2, Genbox.FastData", $true) }
            "Conditional" { return [Type]::GetType("Genbox.FastData.Internal.Structures.ConditionalStructure``2, Genbox.FastData", $true) }
            "HashTable" { return [Type]::GetType("Genbox.FastData.Internal.Structures.HashTableStructure``2, Genbox.FastData", $true) }
            default { throw "Unsupported structure type: $Name" }
        }
    }

    $typeInfo = Get-DataTypeInfo $DataType
    $keyType = $typeInfo[0]
    $converter = $typeInfo[1]
    $lines = @(Get-Content -Path $InputFile)
    $data = [Array]::CreateInstance($keyType, $lines.Count)

    for ($i = 0; $i -lt $lines.Count; $i++)
    {
        $data.SetValue((& $converter $lines[$i]), $i)
    }

    if ($keyType -eq [string])
    {
        $config = [Genbox.FastData.Config.StringDataConfig]::new()
    }
    else
    {
        $config = [Genbox.FastData.Config.NumericDataConfig]::new()
    }

    $structureOverride = Get-StructureTypeOverride $StructureType
    if ($structureOverride -ne $null)
    {
        $config.StructureTypeOverride = $structureOverride
    }

    if ($Command -eq "csharp")
    {
        $cfg = [Genbox.FastData.Generator.CSharp.CSharpCodeGeneratorConfig]::new($ClassName)
        $cfg.Namespace = $Namespace
        $generator = [Genbox.FastData.Generator.CSharp.CSharpCodeGenerator]::new($cfg)
    }
    elseif ($Command -eq "cpp")
    {
        $cfg = [Genbox.FastData.Generator.CPlusPlus.CPlusPlusCodeGeneratorConfig]::new($ClassName)
        $generator = [Genbox.FastData.Generator.CPlusPlus.CPlusPlusCodeGenerator]::new($cfg)
    }
    elseif ($Command -eq "rust")
    {
        $cfg = [Genbox.FastData.Generator.Rust.RustCodeGeneratorConfig]::new($ClassName)
        $generator = [Genbox.FastData.Generator.Rust.RustCodeGenerator]::new($cfg)
    }

    if ($keyType -eq [string])
    {
        $source = [Genbox.FastData.FastDataGenerator]::Generate([string[]]$data, $config, $generator)
    }
    else
    {
        $method = [Genbox.FastData.FastDataGenerator].GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static) |
            Where-Object { $_.Name -eq "Generate" -and $_.IsGenericMethodDefinition -and $_.GetGenericArguments().Length -eq 1 -and $_.GetParameters().Length -eq 4 -and $_.GetParameters()[0].ParameterType.IsArray } |
            Select-Object -First 1

        if ($method -eq $null)
        {
            throw "Unable to find FastDataGenerator.Generate overload"
        }

        $source = $method.MakeGenericMethod($keyType).Invoke($null, @($data, $config, $generator, $null))
    }

    if ($OutputFile)
    {
        $source | Out-File -Encoding UTF8 -FilePath $OutputFile
    }
    else
    {
        Write-Output $source
    }
}

Export-ModuleMember -Function Invoke-FastData