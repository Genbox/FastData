@{
    RootModule        = 'FastData.psm1'
    ModuleVersion     = 'TODO-VERSION'
    GUID              = 'd1a5f1e6-b2df-4e63-8a5c-4f1ec1d345a3'
    Author            = 'Ian Qvist'
    Description       = 'Data structure generator for high-performance lookups of static data'
    PowerShellVersion = '6.0'
    FileList          = @('FastData.psm1',
                          'FastData.psd1',
                          'lib\Genbox.FastData.dll',
                          'lib\Genbox.FastData.Generator.CPlusPlus.dll',
                          'lib\Genbox.FastData.Generator.CSharp.dll',
                          'lib\Genbox.FastData.Generator.dll')
    FunctionsToExport = @('Invoke-FastData')
    PrivateData = @{
        PSData = @{
            Tags = @('FastData', 'Generator')
            ProjectUri = 'https://github.com/Genbox/FastData'
            LicenseUri = 'https://github.com/Genbox/FastData/blob/main/LICENSE.txt'
        }
    }
}