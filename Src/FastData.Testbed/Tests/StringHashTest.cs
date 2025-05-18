using System.Text;
using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Internal.Hashes;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Testbed.Tests;

public static class StringHashTest
{
    public static void TestHash()
    {
        const string str = "hello world";

        CSharpExpressionCompiler c = new CSharpExpressionCompiler(new TypeHelper(new TypeMap(new CSharpLanguageDef().TypeDefinitions)));
        Console.WriteLine(c.GetCode(new DefaultStringHash().BuildExpression()));

        byte[] utf16 = Encoding.Unicode.GetBytes(str);
        byte[] utf8 = Encoding.UTF8.GetBytes(str);

        Console.WriteLine("utf16: " + new DefaultStringHash().GetHashFunction()(ref utf16[0], utf16.Length));
        Console.WriteLine("utf8: " + new DefaultStringHash().GetHashFunction()(ref utf8[0], utf8.Length));

        Console.WriteLine("----------");

        Console.WriteLine("utf16: " + DJB2Hash.ComputeHash(ref utf16[0], utf16.Length));
        Console.WriteLine("utf8: " + DJB2Hash.ComputeHash(ref utf8[0], utf8.Length));
    }
}