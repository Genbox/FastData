function Invoke-FastData
{
    param (
        [Parameter(Mandatory = $true)][ValidateSet("csharp", "cpp")] [string]$Command,
        [string]$OutputFile,
        [ValidateSet("bool", "char", "double", "int16", "int32", "int64", "int8", "single", "string", "uint16", "uint32", "uint64", "uint8")]
        [string]$DataType = "string",
        [string]$Name = "MyData",
        [ValidateSet("Array", "Auto", "BinarySearch", "Conditional", "EytzingerSearch", "HashSetChain", "HashSetLinear", "KeyLength", "PerfectHashBruteForce", "PerfectHashGPerf", "SingleValue")]
        [string]$StructureType = "Auto",
        [Parameter(Mandatory = $true)] [string]$InputFile,
        [string]$Namespace,
        [ValidateSet("Public", "Internal")]
        [string]$ClassVisibility = "Internal",
        [ValidateSet("Instance", "Static", "Struct")]
        [string]$ClassType = "Static"
    )

    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.CSharp.dll" -ErrorAction Stop
    Add-Type -Path "$PSScriptRoot\lib\Genbox.FastData.Generator.CPlusPlus.dll" -ErrorAction Stop

    function Get-TypeFunc
    {
        param ($DataType)
        switch ($DataType)
        {
            "string"  {
                return { [string]$_ }
            }
            "bool"    {
                return { [bool]$_ }
            }
            "char"    {
                return { [char]$_ }
            }
            "int8"    {
                return { [sbyte]::Parse($_) }
            }
            "uint8"   {
                return { [byte]::Parse($_) }
            }
            "int16"   {
                return { [short]::Parse($_) }
            }
            "uint16"  {
                return { [ushort]::Parse($_) }
            }
            "int32"   {
                return { [int]::Parse($_) }
            }
            "uint32"  {
                return { [uint]::Parse($_) }
            }
            "int64"   {
                return { [long]::Parse($_) }
            }
            "uint64"  {
                return { [ulong]::Parse($_) }
            }
            "single"  {
                return { [float]::Parse($_) }
            }
            "double"  {
                return { [double]::Parse($_) }
            }
            default   {
                throw "Unsupported data type: $DataType"
            }
        }
    }

    $typeFunc = Get-TypeFunc $DataType
    $data = Get-Content -Path $InputFile | ForEach-Object { & $typeFunc $_ }
    $config = New-Object Genbox.FastData.Configs.FastDataConfig($Name, $data, [Genbox.FastData.Enums.StructureType]::$StructureType)

    if ($Command -eq "csharp")
    {
        $cfg = New-Object Genbox.FastData.Generator.CSharp.CSharpGeneratorConfig
        $cfg.Namespace = $Namespace
        $cfg.ClassVisibility = [Genbox.FastData.Generator.CSharp.Enums.ClassVisibility]::$ClassVisibility
        $cfg.ClassType = [Genbox.FastData.Generator.CSharp.Enums.ClassType]::$ClassType
        $generator = New-Object Genbox.FastData.Generator.CSharp.CSharpCodeGenerator $cfg
    }
    elseif ($Command -eq "cpp")
    {
        $cfg = New-Object Genbox.FastData.Generator.CPlusPlus.CPlusPlusGeneratorConfig
        $generator = New-Object Genbox.FastData.Generator.CPlusPlus.CPlusPlusCodeGenerator $cfg
    }

    $source = [ref]''
    $ok = [Genbox.FastData.FastDataGenerator]::TryGenerate($config, $generator, [ref]$source)

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
