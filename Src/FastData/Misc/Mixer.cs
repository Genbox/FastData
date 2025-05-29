using System.Linq.Expressions;

namespace Genbox.FastData.Misc;

public delegate Expression Mixer(Expression hash, Expression readFunc);