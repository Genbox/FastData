using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "xUnit requires public test and fixture types for discovery.",
    Scope = "namespaceanddescendants",
    Target = "~N:Genbox.FastData.TestHarness.Runner.Tests")]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "xUnit class data must remain public for discovery by public theory methods.",
    Scope = "namespaceanddescendants",
    Target = "~N:Genbox.FastData.TestHarness.Runner.Code.Theory")]