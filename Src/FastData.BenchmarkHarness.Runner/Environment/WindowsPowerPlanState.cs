namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal readonly record struct WindowsPowerPlanState(Guid PreviousPowerScheme, uint PreviousProcessorMinAc, uint PreviousProcessorMaxAc, string PowerPlanDisplay, string ProcessorAcDisplay);