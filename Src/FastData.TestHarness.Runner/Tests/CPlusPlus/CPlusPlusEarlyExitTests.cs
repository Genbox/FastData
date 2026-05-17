using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

[Collection("Docker-CPlusPlus")]
public sealed class CPlusPlusEarlyExitTests(DockerCPlusPlusFixture fixture) : EarlyExitTestBase(new CPlusPlusTest(fixture.DockerManager), Bootstrap.CreateExpressionCompiler())
{
    private static readonly CPlusPlusBootstrap Bootstrap = new CPlusPlusBootstrap(HarnessType.Test);

    protected override string RenderProgram(string expression, object value) =>
        $$"""
          #include <cstdint>
          #include <string_view>

          static uint32_t ToAsciiLower(uint32_t value)
          {
              const uint32_t candidate = value | 0x20u;
              return candidate - 'a' <= 'z' - 'a' ? candidate : value;
          }

          static uint32_t UnitAt(std::string_view str, int32_t offset) { const size_t index = offset >= 0 ? static_cast<size_t>(offset) : str.length() + offset; return static_cast<unsigned char>(str[index]); }
          static uint32_t UnitAtAsciiLower(std::string_view str, int32_t offset) { return ToAsciiLower(UnitAt(str, offset)); }
          static int32_t Length(std::string_view str) { return static_cast<int32_t>(str.length()); }

          static bool EqualsAt(std::string_view str, int32_t offset, std::string_view fragment)
          {
              size_t start = offset >= 0 ? static_cast<size_t>(offset) : str.length() + offset;
              return str.compare(start, fragment.length(), fragment) == 0;
          }

          static bool EqualsAtAsciiLower(std::string_view str, int32_t offset, std::string_view fragment)
          {
              size_t start = offset >= 0 ? static_cast<size_t>(offset) : str.length() + offset;
              for (size_t i = 0; i < fragment.length(); ++i)
              {
                  if (ToAsciiLower(static_cast<unsigned char>(str[start + i])) != ToAsciiLower(static_cast<unsigned char>(fragment[i])))
                      return false;
              }
              return true;
          }

          {{Bootstrap.Wrap($"""
                                    {Bootstrap.Map.GetTypeName(value.GetType())} inputKey = {Bootstrap.Map.ToValueLabel(value)};
                                    return {expression} ? 1 : 0;
                            """)}}
          """;
}