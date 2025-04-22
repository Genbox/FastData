namespace Genbox.FastData.Generator.Rust.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config, bool asStatic = false) => config.DataType switch
    {
        DataType.String when asStatic => "&'static str",
        DataType.String => "&str",
        DataType.Boolean => "bool",
        DataType.SByte => "i8",
        DataType.Byte => "u8",
        DataType.Char => "char",
        DataType.Int16 => "i16",
        DataType.UInt16 => "u16",
        DataType.Int32 => "i32",
        DataType.UInt32 => "u32",
        DataType.Int64 => "i64",
        DataType.UInt64 => "u64",
        DataType.Single => "f32",
        DataType.Double => "f64",
        _ => throw new InvalidOperationException("Invalid DataType: " + config.DataType)
    };

    internal static string GetEqualFunction(this GeneratorConfig config, string variable, bool asRef = false) => $"value == {(asRef ? "*" : "")}{variable}";

    internal static string GetCompareFunction(this GeneratorConfig config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"{variable}.cmp(value) as i32";

        return $"if value > {variable} {{ 1 }} else if value < {variable} {{ -1 }} else {{ 0 }}";
    }

    internal static string GetHashSource(this GeneratorConfig config)
    {
        if (config.DataType == DataType.String)
        {
            return $$"""
                         fn get_hash(value: &str) -> u32 {
                             let mut hash1: u32 = (5381 << 16) + 5381;
                             let mut hash2: u32 = (5381 << 16) + 5381;

                             for chunk in value.as_bytes().chunks(8) {
                                 if chunk.len() >= 4 {
                                     let part1 = u32::from_le_bytes(chunk[0..4].try_into().unwrap());
                                     hash1 = hash1.rotate_left(5) ^ part1;
                                 }
                                 if chunk.len() == 8 {
                                     let part2 = u32::from_le_bytes(chunk[4..8].try_into().unwrap());
                                     hash2 = hash2.rotate_left(5) ^ part2;
                                 }
                             }

                             for &b in value.as_bytes().iter().skip(value.len() / 8 * 8) {
                                 hash2 = hash2.rotate_left(5) ^ b as u32;
                             }

                             hash1.wrapping_add(hash2.wrapping_mul(0x5D588B65))
                         }
                     """;
        }

        return $$"""
                     fn get_hash(value: {{config.GetTypeName()}}) -> u32 {
                         value as u32
                     }
                 """;
    }
}