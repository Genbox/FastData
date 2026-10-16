using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException.ThrowIf", Justification = "FastData targets netstandard2.0, where ArgumentNullException.ThrowIfNull is unavailable; explicit guards keep one implementation across targets.")]