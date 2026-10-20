using System.Globalization;
using System.Text;

namespace Genbox.FastData.Ir.Validation;

// Paths share immutable segments and materialize diagnostic text only when needed.
internal readonly struct IrDiagnosticPath
{
    private readonly Segment? _path;

    private IrDiagnosticPath(Segment path)
    {
        _path = path;
    }

    public bool IsEmpty => _path == null;
    public static IrDiagnosticPath Root(string name = "Program") => new IrDiagnosticPath(new Segment(null, name));
    public IrDiagnosticPath Index(string member, int index) => IsEmpty ? default : Member(member, $"[{index.ToString(CultureInfo.InvariantCulture)}]");

    public IrDiagnosticPath Named(string member, string? name, int id)
    {
        if (IsEmpty)
            return default;

        if (name == null)
            return Index(member, id);

        return Member(member, $"[{name}#{id.ToString(CultureInfo.InvariantCulture)}]");
    }

    public static IrDiagnosticPath operator +(IrDiagnosticPath path, string suffix) => path.IsEmpty ? default : new IrDiagnosticPath(new Segment(path._path, suffix));

    public override string ToString()
    {
        if (_path == null)
            return "Program";

        Stack<string> parts = new Stack<string>();
        for (Segment? part = _path; part != null; part = part.Parent)
            parts.Push(part.Text);
        StringBuilder text = new StringBuilder();
        while (parts.Count > 0)
            text.Append(parts.Pop());
        return text.ToString();
    }

    private IrDiagnosticPath Member(string member, string suffix) => this + ("." + member + suffix);

    private sealed class Segment(Segment? parent, string text)
    {
        public Segment? Parent { get; } = parent;
        public string Text { get; } = text;
    }
}