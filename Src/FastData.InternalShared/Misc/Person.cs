namespace Genbox.FastData.InternalShared.Misc;

internal class Person
{
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
    public Person? Other { get; set; }
}