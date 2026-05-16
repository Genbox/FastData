using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Pgm;

/// <remarks>
/// Based on the reference OptimalPiecewiseLinearModel (piecewise_linear_model.hpp).
/// The reference uses a single C++ template parameterized on LargeSigned&lt;T&gt; with if-constexpr
/// to branch between integral and floating-point paths. We use separate FloatingModel (double) and
/// IntegralModel (Int128) implementations to achieve the same effect in C#. The algorithm and
/// GetFloatingPointSegment logic match the reference for each respective code path.
/// Epsilon is not defensively validated (by design).
/// </remarks>
[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
internal sealed class OptimalPiecewiseLinearModel<T> where T : notnull
{
    private readonly IModel _model;

    public OptimalPiecewiseLinearModel(int epsilon)
    {
        if (PgmTypeTraits<T>.IsFloatingPoint)
            _model = new FloatingModel(epsilon);
        else
            _model = new IntegralModel(epsilon);
    }

    public bool AddPoint(T xValue, int y) => _model.AddPoint(xValue, y);

    public CanonicalSegment GetSegment() => new CanonicalSegment(_model.GetSegment());

    public void Reset() => _model.Reset();

    internal sealed class CanonicalSegment
    {
        private readonly ICanonicalSegment _inner;

        internal CanonicalSegment(ICanonicalSegment inner)
        {
            _inner = inner;
        }

        public T GetFirstKey() => _inner.GetFirstKey();

        public (double slope, double intercept) GetFloatingPointSegment(T origin)
            => _inner.GetFloatingPointSegment(origin);

        public (double minSlope, double maxSlope) GetSlopeRange() => _inner.GetSlopeRange();

        public (double x, double y) GetIntersection() => _inner.GetIntersection();
    }

    private interface IModel
    {
        bool AddPoint(T xValue, int y);
        ICanonicalSegment GetSegment();
        void Reset();
    }

    internal interface ICanonicalSegment
    {
        T GetFirstKey();
        (double slope, double intercept) GetFloatingPointSegment(T origin);
        (double minSlope, double maxSlope) GetSlopeRange();
        (double x, double y) GetIntersection();
    }

    private sealed class FloatingModel(int epsilon) : IModel
    {
        private readonly List<Point> _lower = new List<Point>(1 << 16);
        private readonly Point[] _rectangle = new Point[4];
        private readonly List<Point> _upper = new List<Point>(1 << 16);
        private double _firstX;
        private double _lastX;
        private int _lowerStart;
        private int _pointsInHull;
        private int _upperStart;

        public bool AddPoint(T xValue, int y)
        {
            double x = PgmTypeTraits<T>.ToDouble(xValue);
            if (_pointsInHull > 0 && x <= _lastX)
                throw new InvalidOperationException("Points must be increasing by x.");

            _lastX = x;
            const int maxY = int.MaxValue;
            const int minY = 0;
            int yPlus = y >= maxY - epsilon ? maxY : y + epsilon;
            int yMinus = y <= minY + epsilon ? minY : y - epsilon;
            Point p1 = new Point(x, yPlus);
            Point p2 = new Point(x, yMinus);

            if (_pointsInHull == 0)
            {
                _firstX = x;
                _rectangle[0] = p1;
                _rectangle[1] = p2;
                _upper.Clear();
                _lower.Clear();
                _upper.Add(p1);
                _lower.Add(p2);
                _upperStart = 0;
                _lowerStart = 0;
                _pointsInHull++;
                return true;
            }

            if (_pointsInHull == 1)
            {
                _rectangle[2] = p2;
                _rectangle[3] = p1;
                _upper.Add(p1);
                _lower.Add(p2);
                _pointsInHull++;
                return true;
            }

            Slope slope1 = _rectangle[2] - _rectangle[0];
            Slope slope2 = _rectangle[3] - _rectangle[1];
            bool outsideLine1 = (p1 - _rectangle[2]).CompareTo(slope1) < 0;
            bool outsideLine2 = (p2 - _rectangle[3]).CompareTo(slope2) > 0;

            if (outsideLine1 || outsideLine2)
            {
                _pointsInHull = 0;
                return false;
            }

            if ((p1 - _rectangle[1]).CompareTo(slope2) < 0)
            {
                Slope min = _lower[_lowerStart] - p1;
                int minIndex = _lowerStart;
                for (int i = _lowerStart + 1; i < _lower.Count; i++)
                {
                    Slope val = _lower[i] - p1;
                    if (val.CompareTo(min) > 0)
                        break;
                    min = val;
                    minIndex = i;
                }

                _rectangle[1] = _lower[minIndex];
                _rectangle[3] = p1;
                _lowerStart = minIndex;

                int end = _upper.Count;
                while (end >= _upperStart + 2 && Cross(_upper[end - 2], _upper[end - 1], p1) <= 0)
                    end--;
                if (end < _upper.Count)
                    _upper.RemoveRange(end, _upper.Count - end);
                _upper.Add(p1);
            }

            if ((p2 - _rectangle[0]).CompareTo(slope1) > 0)
            {
                Slope max = _upper[_upperStart] - p2;
                int maxIndex = _upperStart;
                for (int i = _upperStart + 1; i < _upper.Count; i++)
                {
                    Slope val = _upper[i] - p2;
                    if (val.CompareTo(max) < 0)
                        break;
                    max = val;
                    maxIndex = i;
                }

                _rectangle[0] = _upper[maxIndex];
                _rectangle[2] = p2;
                _upperStart = maxIndex;

                int end = _lower.Count;
                while (end >= _lowerStart + 2 && Cross(_lower[end - 2], _lower[end - 1], p2) >= 0)
                    end--;
                if (end < _lower.Count)
                    _lower.RemoveRange(end, _lower.Count - end);
                _lower.Add(p2);
            }

            _pointsInHull++;
            return true;
        }

        public ICanonicalSegment GetSegment()
        {
            if (_pointsInHull == 1)
                return new FloatingCanonicalSegment(_rectangle[0], _rectangle[1], _firstX);
            return new FloatingCanonicalSegment(_rectangle, _firstX);
        }

        public void Reset()
        {
            _pointsInHull = 0;
            _lower.Clear();
            _upper.Clear();
        }

        private static double Cross(in Point o, in Point a, in Point b)
        {
            Slope oa = a - o;
            Slope ob = b - o;
            return (oa.Dx * ob.Dy) - (oa.Dy * ob.Dx);
        }

        private sealed class FloatingCanonicalSegment : ICanonicalSegment
        {
            private readonly double _first;
            private readonly Point[] _rectangle;

            public FloatingCanonicalSegment(Point p0, Point p1, double first)
            {
                _rectangle = [p0, p1, p0, p1];
                _first = first;
            }

            public FloatingCanonicalSegment(Point[] rectangle, double first)
            {
                _rectangle = [rectangle[0], rectangle[1], rectangle[2], rectangle[3]];
                _first = first;
            }

            public T GetFirstKey() => PgmTypeTraits<T>.FromDouble(_first);

            public (double slope, double intercept) GetFloatingPointSegment(T origin)
            {
                if (OnePoint())
                    return (0, (_rectangle[0].Y + _rectangle[1].Y) / 2.0);

                double originValue = PgmTypeTraits<T>.ToDouble(origin);
                (double minSlope, double maxSlope) = GetSlopeRange();
                (double ix, double iy) = GetIntersection();
                double slope = (minSlope + maxSlope) / 2.0;
                double intercept = iy - ((ix - originValue) * slope);
                return (slope, intercept);
            }

            public (double minSlope, double maxSlope) GetSlopeRange()
            {
                if (OnePoint())
                    return (0, 1);
                double minSlope = (double)(_rectangle[2] - _rectangle[0]);
                double maxSlope = (double)(_rectangle[3] - _rectangle[1]);
                return (minSlope, maxSlope);
            }

            public (double x, double y) GetIntersection()
            {
                Point p0 = _rectangle[0];
                Point p1 = _rectangle[1];
                Point p2 = _rectangle[2];
                Point p3 = _rectangle[3];
                Slope slope1 = p2 - p0;
                Slope slope2 = p3 - p1;

                if (OnePoint() || slope1.CompareTo(slope2) == 0)
                    return (p0.X, p0.Y);

                Slope p0p1 = p1 - p0;
                double a = (slope1.Dx * slope2.Dy) - (slope1.Dy * slope2.Dx);
                double b = ((p0p1.Dx * slope2.Dy) - (p0p1.Dy * slope2.Dx)) / a;
                double ix = p0.X + (b * slope1.Dx);
                double iy = p0.Y + (b * slope1.Dy);
                return (ix, iy);
            }

            [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
            private bool OnePoint() => _rectangle[0].X == _rectangle[2].X && _rectangle[0].Y == _rectangle[2].Y
                                                                          && _rectangle[1].X == _rectangle[3].X && _rectangle[1].Y == _rectangle[3].Y;
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Point(double x, double y)
        {
            public double X { get; } = x;
            public double Y { get; } = y;

            public static Slope operator -(Point a, Point b) => new Slope(a.X - b.X, a.Y - b.Y);
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Slope(double dx, double dy) : IComparable<Slope>
        {
            public double Dx { get; } = dx;
            public double Dy { get; } = dy;

            public int CompareTo(Slope other)
            {
                double left = Dy * other.Dx;
                double right = Dx * other.Dy;
                return left.CompareTo(right);
            }

            public static explicit operator double(Slope s) => s.Dx == 0 ? 0 : s.Dy / s.Dx;
        }
    }

    private sealed class IntegralModel : IModel
    {
        private readonly int _epsilon;
        private readonly List<Point> _lower;
        private readonly Point[] _rectangle;
        private readonly List<Point> _upper;
        private Int128 _firstX;
        private Int128 _lastX;
        private int _lowerStart;
        private int _pointsInHull;
        private int _upperStart;

        public IntegralModel(int epsilon)
        {
            _epsilon = epsilon;
            _lower = new List<Point>(1 << 16);
            _upper = new List<Point>(1 << 16);
            _rectangle = new Point[4];
        }

        public bool AddPoint(T xValue, int y)
        {
            Int128 x = PgmTypeTraits<T>.ToInt128(xValue);
            if (_pointsInHull > 0 && x <= _lastX)
                throw new InvalidOperationException("Points must be increasing by x.");

            _lastX = x;
            const int maxY = int.MaxValue;
            const int minY = 0;
            int yPlus = y >= maxY - _epsilon ? maxY : y + _epsilon;
            int yMinus = y <= minY + _epsilon ? minY : y - _epsilon;
            Point p1 = new Point(x, yPlus);
            Point p2 = new Point(x, yMinus);

            if (_pointsInHull == 0)
            {
                _firstX = x;
                _rectangle[0] = p1;
                _rectangle[1] = p2;
                _upper.Clear();
                _lower.Clear();
                _upper.Add(p1);
                _lower.Add(p2);
                _upperStart = 0;
                _lowerStart = 0;
                _pointsInHull++;
                return true;
            }

            if (_pointsInHull == 1)
            {
                _rectangle[2] = p2;
                _rectangle[3] = p1;
                _upper.Add(p1);
                _lower.Add(p2);
                _pointsInHull++;
                return true;
            }

            Slope slope1 = _rectangle[2] - _rectangle[0];
            Slope slope2 = _rectangle[3] - _rectangle[1];
            bool outsideLine1 = (p1 - _rectangle[2]).CompareTo(slope1) < 0;
            bool outsideLine2 = (p2 - _rectangle[3]).CompareTo(slope2) > 0;

            if (outsideLine1 || outsideLine2)
            {
                _pointsInHull = 0;
                return false;
            }

            if ((p1 - _rectangle[1]).CompareTo(slope2) < 0)
            {
                Slope min = _lower[_lowerStart] - p1;
                int minIndex = _lowerStart;
                for (int i = _lowerStart + 1; i < _lower.Count; i++)
                {
                    Slope val = _lower[i] - p1;
                    if (val.CompareTo(min) > 0)
                        break;
                    min = val;
                    minIndex = i;
                }

                _rectangle[1] = _lower[minIndex];
                _rectangle[3] = p1;
                _lowerStart = minIndex;

                int end = _upper.Count;
                while (end >= _upperStart + 2 && Cross(_upper[end - 2], _upper[end - 1], p1) <= 0)
                    end--;
                if (end < _upper.Count)
                    _upper.RemoveRange(end, _upper.Count - end);
                _upper.Add(p1);
            }

            if ((p2 - _rectangle[0]).CompareTo(slope1) > 0)
            {
                Slope max = _upper[_upperStart] - p2;
                int maxIndex = _upperStart;
                for (int i = _upperStart + 1; i < _upper.Count; i++)
                {
                    Slope val = _upper[i] - p2;
                    if (val.CompareTo(max) < 0)
                        break;
                    max = val;
                    maxIndex = i;
                }

                _rectangle[0] = _upper[maxIndex];
                _rectangle[2] = p2;
                _upperStart = maxIndex;

                int end = _lower.Count;
                while (end >= _lowerStart + 2 && Cross(_lower[end - 2], _lower[end - 1], p2) >= 0)
                    end--;
                if (end < _lower.Count)
                    _lower.RemoveRange(end, _lower.Count - end);
                _lower.Add(p2);
            }

            _pointsInHull++;
            return true;
        }

        public ICanonicalSegment GetSegment()
        {
            if (_pointsInHull == 1)
                return new IntegralCanonicalSegment(_rectangle[0], _rectangle[1], _firstX);
            return new IntegralCanonicalSegment(_rectangle, _firstX);
        }

        public void Reset()
        {
            _pointsInHull = 0;
            _lower.Clear();
            _upper.Clear();
        }

        private static Int128 Cross(in Point o, in Point a, in Point b)
        {
            Slope oa = a - o;
            Slope ob = b - o;
            return (oa.Dx * ob.Dy) - (oa.Dy * ob.Dx);
        }

        private sealed class IntegralCanonicalSegment : ICanonicalSegment
        {
            private readonly Int128 _first;
            private readonly Point[] _rectangle;

            public IntegralCanonicalSegment(Point p0, Point p1, Int128 first)
            {
                _rectangle = [p0, p1, p0, p1];
                _first = first;
            }

            public IntegralCanonicalSegment(Point[] rectangle, Int128 first)
            {
                _rectangle = [rectangle[0], rectangle[1], rectangle[2], rectangle[3]];
                _first = first;
            }

            public T GetFirstKey() => PgmTypeTraits<T>.FromInt128(_first);

            public (double slope, double intercept) GetFloatingPointSegment(T origin)
            {
                if (OnePoint())
                    return (0, (double)((_rectangle[0].Y + _rectangle[1].Y) / 2));

                Slope slope = _rectangle[3] - _rectangle[1];
                Int128 originValue = PgmTypeTraits<T>.ToInt128(origin);
                Int128 interceptN = slope.Dy * (originValue - _rectangle[1].X);
                Int128 interceptD = slope.Dx;
                Int128 roundingTerm = ((interceptN < 0) ^ (interceptD < 0) ? -1 : 1) * (interceptD / 2);
                Int128 intercept = ((interceptN + roundingTerm) / interceptD) + _rectangle[1].Y;
                return ((double)slope, (double)intercept);
            }

            public (double minSlope, double maxSlope) GetSlopeRange()
            {
                if (OnePoint())
                    return (0, 1);
                double minSlope = (double)(_rectangle[2] - _rectangle[0]);
                double maxSlope = (double)(_rectangle[3] - _rectangle[1]);
                return (minSlope, maxSlope);
            }

            public (double x, double y) GetIntersection()
            {
                Point p0 = _rectangle[0];
                Point p1 = _rectangle[1];
                Point p2 = _rectangle[2];
                Point p3 = _rectangle[3];
                Slope slope1 = p2 - p0;
                Slope slope2 = p3 - p1;

                if (OnePoint() || slope1.CompareTo(slope2) == 0)
                    return ((double)p0.X, (double)p0.Y);

                Slope p0p1 = p1 - p0;
                Int128 a = (slope1.Dx * slope2.Dy) - (slope1.Dy * slope2.Dx);
                Int128 numerator = (p0p1.Dx * slope2.Dy) - (p0p1.Dy * slope2.Dx);
                double b = (double)numerator / (double)a;
                double ix = (double)p0.X + (b * (double)slope1.Dx);
                double iy = (double)p0.Y + (b * (double)slope1.Dy);
                return (ix, iy);
            }

            private bool OnePoint() => _rectangle[0].X == _rectangle[2].X && _rectangle[0].Y == _rectangle[2].Y
                                                                          && _rectangle[1].X == _rectangle[3].X && _rectangle[1].Y == _rectangle[3].Y;
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Point(Int128 x, Int128 y)
        {
            public Int128 X { get; } = x;
            public Int128 Y { get; } = y;

            public static Slope operator -(Point a, Point b) => new Slope(a.X - b.X, a.Y - b.Y);
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Slope(Int128 dx, Int128 dy) : IComparable<Slope>
        {
            public Int128 Dx { get; } = dx;
            public Int128 Dy { get; } = dy;

            public int CompareTo(Slope other)
            {
                Int128 left = Dy * other.Dx;
                Int128 right = Dx * other.Dy;
                return left.CompareTo(right);
            }

            public static explicit operator double(Slope s)
            {
                if (s.Dx == 0)
                    return 0;
                return (double)s.Dy / (double)s.Dx;
            }
        }
    }
}