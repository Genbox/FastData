using Genbox.FastData.Generator.Ir.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.Ir.CSharp.TestHarness;
using Genbox.FastData.Generator.Ir.Rust.TestHarness;
using Genbox.FastData.Ir.Harness.Runner;

return await HarnessApplication.RunAsync(args, true, new CSharpHarnessBackend(), new RustHarnessBackend(), new CPlusPlusHarnessBackend());