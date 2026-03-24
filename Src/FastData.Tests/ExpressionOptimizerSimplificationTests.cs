using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions.Optimizer;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Tests;

public sealed class ExpressionOptimizerSimplificationTests
{
    [Fact]
    public async Task FoldsBasicConstantMath()
    {
        // Before: 2 + 3
        // After: 5
        BinaryExpression expression = Add(Constant(2), Constant(3));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task RemovesAdditiveIdentity()
    {
        // Before: x + 0
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Add(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task RemovesAdditiveIdentityLeft()
    {
        // Before: 0 + x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Add(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task RemovesMultiplicativeIdentity()
    {
        // Before: 1 * x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(Constant(1), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task RemovesMultiplicativeIdentityRight()
    {
        // Before: x * 1
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(parameter, Constant(1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMultiplyByZeroRight()
    {
        // Before: x * 0
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMultiplyByZeroLeft()
    {
        // Before: 0 * x
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesSubtractSameExpression()
    {
        // Before: x - x
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Subtract(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesSubtractZero()
    {
        // Before: x - 0
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Subtract(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesDivisionByOne()
    {
        // Before: x / 1
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Divide(parameter, Constant(1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesModuloByOne()
    {
        // Before: x % 1
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Modulo(parameter, Constant(1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesConditionalToTest()
    {
        // Before: x > 0 ? true : false
        // After: x > 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression test = GreaterThan(parameter, Constant(0));
        ConditionalExpression expression = Condition(test, Constant(true), Constant(false));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesConditionalWithSameBranches()
    {
        // Before: g ? x : x
        // After: x
        ParameterExpression guard = Parameter(typeof(bool), "g");
        ParameterExpression parameter = Parameter(typeof(int), "x");
        ConditionalExpression expression = Condition(guard, parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task FoldsNestedAddConstants()
    {
        // Before: (x + 2) + 3
        // After: x + 5
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Add(Add(parameter, Constant(2)), Constant(3));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task FoldsConstantComparison()
    {
        // Before: 3 > 2
        // After: true
        BinaryExpression expression = GreaterThan(Constant(3), Constant(2));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task RangeOptimizationAndAlsoKeepsHigherBound()
    {
        // Before: (x > 3) && (x > 5)
        // After: x > 5
        ParameterExpression param = Parameter(typeof(int), "x");
        ConstantExpression const3 = Constant(3);
        ConstantExpression const5 = Constant(5);
        BinaryExpression greaterThan3 = GreaterThan(param, const3);
        BinaryExpression greaterThan5 = GreaterThan(param, const5);
        BinaryExpression combined = AndAlso(greaterThan3, greaterThan5);

        await OptimizeAsync(combined);
    }

    [Fact]
    public async Task RangeOptimizationOrElseKeepsLowerBound()
    {
        // Before: (x > 3) || (x > 5)
        // After: x > 3
        ParameterExpression param = Parameter(typeof(int), "x");
        ConstantExpression const3 = Constant(3);
        ConstantExpression const5 = Constant(5);
        BinaryExpression greaterThan3 = GreaterThan(param, const3);
        BinaryExpression greaterThan5 = GreaterThan(param, const5);
        BinaryExpression combined = OrElse(greaterThan3, greaterThan5);

        await OptimizeAsync(combined);
    }

    [Fact]
    public async Task RangeOptimizationAndAlsoKeepsHigherBoundWhenReversed()
    {
        // Before: (x > 5) && (x > 3)
        // After: x > 5
        ParameterExpression param = Parameter(typeof(int), "x");
        ConstantExpression const3 = Constant(3);
        ConstantExpression const5 = Constant(5);
        BinaryExpression greaterThan3 = GreaterThan(param, const3);
        BinaryExpression greaterThan5 = GreaterThan(param, const5);
        BinaryExpression combined = AndAlso(greaterThan5, greaterThan3);

        await OptimizeAsync(combined);
    }

    [Fact]
    public async Task FactorComplementReducesPAndQOrNotPAndQ()
    {
        // Before: (P && Q) || (!P && Q)
        // After: Q
        ParameterExpression paramP = Parameter(typeof(bool), "P");
        ParameterExpression paramQ = Parameter(typeof(bool), "Q");
        UnaryExpression notP = Not(paramP);
        BinaryExpression pAndQ = AndAlso(paramP, paramQ);
        BinaryExpression notPAndQ = AndAlso(notP, paramQ);
        BinaryExpression test = OrElse(pAndQ, notPAndQ);

        await OptimizeAsync(test);
    }

    [Fact]
    public async Task FactorComplementReducesQAndPOrQAndNotP()
    {
        // Before: (Q && P) || (Q && !P)
        // After: Q
        ParameterExpression paramP = Parameter(typeof(bool), "P");
        ParameterExpression paramQ = Parameter(typeof(bool), "Q");
        UnaryExpression notP = Not(paramP);
        BinaryExpression qAndP = AndAlso(paramQ, paramP);
        BinaryExpression qAndNotP = AndAlso(paramQ, notP);
        BinaryExpression test = OrElse(qAndP, qAndNotP);

        await OptimizeAsync(test);
    }

    [Fact]
    public async Task FactorComplementReducesNotPAndROrPAndR()
    {
        // Before: (!P && R) || (P && R)
        // After: R
        ParameterExpression paramP = Parameter(typeof(bool), "P");
        ParameterExpression paramR = Parameter(typeof(bool), "R");
        UnaryExpression notP = Not(paramP);
        BinaryExpression notPAndR = AndAlso(notP, paramR);
        BinaryExpression pAndR = AndAlso(paramP, paramR);
        BinaryExpression test = OrElse(notPAndR, pAndR);

        await OptimizeAsync(test);
    }

    [Fact]
    public async Task FactorComplementReducesRAndNotQOrRAndQ()
    {
        // Before: (R && !Q) || (R && Q)
        // After: R
        ParameterExpression paramQ = Parameter(typeof(bool), "Q");
        ParameterExpression paramR = Parameter(typeof(bool), "R");
        UnaryExpression notQ = Not(paramQ);
        BinaryExpression rAndNotQ = AndAlso(paramR, notQ);
        BinaryExpression rAndQ = AndAlso(paramR, paramQ);
        BinaryExpression test = OrElse(rAndNotQ, rAndQ);

        await OptimizeAsync(test);
    }

    [Fact]
    public async Task PreservesBitwiseOrOnInts()
    {
        // Before: 1 | 2
        // After: 1 | 2
        BinaryExpression expression = Or(Constant(1), Constant(2));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task OptimizesMemberInitBindings()
    {
        // Before: new BoolHolder { Value = (p && true) }
        // After: new BoolHolder { Value = p }
        ParameterExpression param = Parameter(typeof(bool), "p");
        PropertyInfo property = typeof(BoolHolder).GetProperty(nameof(BoolHolder.Value), BindingFlags.Instance | BindingFlags.Public)!;
        MemberAssignment binding = Bind(property, AndAlso(param, Constant(true)));
        MemberInitExpression init = MemberInit(New(typeof(BoolHolder)), binding);

        await OptimizeAsync(init);
    }

    [Fact]
    public async Task OptimizesListInitArguments()
    {
        // Before: new List<bool> { (p || false) }
        // After: new List<bool> { p }
        ParameterExpression param = Parameter(typeof(bool), "p");
        MethodInfo addMethod = typeof(List<bool>).GetMethod("Add", new[] { typeof(bool) })!;
        ElementInit initializer = ElementInit(addMethod, OrElse(param, Constant(false)));
        ListInitExpression init = ListInit(New(typeof(List<bool>)), initializer);

        await OptimizeAsync(init);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndSelf()
    {
        // Before: x & x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = And(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrSelf()
    {
        // Before: x | x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Or(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorSelf()
    {
        // Before: x ^ x
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = ExclusiveOr(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndZeroRight()
    {
        // Before: x & 0
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = And(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndZeroLeft()
    {
        // Before: 0 & x
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = And(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrZeroRight()
    {
        // Before: x | 0
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Or(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrZeroLeft()
    {
        // Before: 0 | x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Or(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorZeroRight()
    {
        // Before: x ^ 0
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = ExclusiveOr(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorZeroLeft()
    {
        // Before: 0 ^ x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = ExclusiveOr(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndAllOnesRightSigned()
    {
        // Before: x & -1
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = And(parameter, Constant(-1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndAllOnesLeftSigned()
    {
        // Before: -1 & x
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = And(Constant(-1), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrAllOnesRightSigned()
    {
        // Before: x | -1
        // After: -1
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Or(parameter, Constant(-1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrAllOnesLeftSigned()
    {
        // Before: -1 | x
        // After: -1
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Or(Constant(-1), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorAllOnesRightSigned()
    {
        // Before: x ^ -1
        // After: ~x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = ExclusiveOr(parameter, Constant(-1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorAllOnesLeftSigned()
    {
        // Before: -1 ^ x
        // After: ~x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = ExclusiveOr(Constant(-1), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndAllOnesUnsigned()
    {
        // Before: x & uint.MaxValue
        // After: x
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = And(parameter, Constant(uint.MaxValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseAndAllOnesUnsignedLeft()
    {
        // Before: uint.MaxValue & x
        // After: x
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = And(Constant(uint.MaxValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrAllOnesUnsigned()
    {
        // Before: x | uint.MaxValue
        // After: uint.MaxValue
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = Or(parameter, Constant(uint.MaxValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseOrAllOnesUnsignedLeft()
    {
        // Before: uint.MaxValue | x
        // After: uint.MaxValue
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = Or(Constant(uint.MaxValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorAllOnesUnsigned()
    {
        // Before: x ^ uint.MaxValue
        // After: ~x
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = ExclusiveOr(parameter, Constant(uint.MaxValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBitwiseXorAllOnesUnsignedLeft()
    {
        // Before: uint.MaxValue ^ x
        // After: ~x
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = ExclusiveOr(Constant(uint.MaxValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesXorCancellationLeft()
    {
        // Before: (x ^ y) ^ x
        // After: y
        ParameterExpression x = Parameter(typeof(int), "x");
        ParameterExpression y = Parameter(typeof(int), "y");
        BinaryExpression inner = ExclusiveOr(x, y);
        BinaryExpression expression = ExclusiveOr(inner, x);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesXorCancellationRight()
    {
        // Before: x ^ (y ^ x)
        // After: y
        ParameterExpression x = Parameter(typeof(int), "x");
        ParameterExpression y = Parameter(typeof(int), "y");
        BinaryExpression inner = ExclusiveOr(y, x);
        BinaryExpression expression = ExclusiveOr(x, inner);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesXorCancellationAlternative()
    {
        // Before: (x ^ y) ^ y
        // After: x
        ParameterExpression x = Parameter(typeof(int), "x");
        ParameterExpression y = Parameter(typeof(int), "y");
        BinaryExpression inner = ExclusiveOr(x, y);
        BinaryExpression expression = ExclusiveOr(inner, y);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesLeftShiftByZero()
    {
        // Before: x << 0
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LeftShift(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesRightShiftByZero()
    {
        // Before: x >> 0
        // After: x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = RightShift(parameter, Constant(0));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesZeroLeftShift()
    {
        // Before: 0 << x
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LeftShift(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesZeroRightShift()
    {
        // Before: 0 >> x
        // After: 0
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = RightShift(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesLessThanSelf()
    {
        // Before: x < x
        // After: false
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LessThan(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesLessThanOrEqualSelf()
    {
        // Before: x <= x
        // After: true
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LessThanOrEqual(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesGreaterThanSelf()
    {
        // Before: x > x
        // After: false
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = GreaterThan(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesGreaterThanOrEqualSelf()
    {
        // Before: x >= x
        // After: true
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = GreaterThanOrEqual(parameter, parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMinLessThanOrEqual()
    {
        // Before: int.MinValue <= x
        // After: true
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LessThanOrEqual(Constant(int.MinValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMinGreaterThan()
    {
        // Before: int.MinValue > x
        // After: false
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = GreaterThan(Constant(int.MinValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMaxGreaterThanOrEqual()
    {
        // Before: int.MaxValue >= x
        // After: true
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = GreaterThanOrEqual(Constant(int.MaxValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMaxLessThan()
    {
        // Before: int.MaxValue < x
        // After: false
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LessThan(Constant(int.MaxValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesLessThanMin()
    {
        // Before: x < int.MinValue
        // After: false
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LessThan(parameter, Constant(int.MinValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesGreaterThanOrEqualMin()
    {
        // Before: x >= int.MinValue
        // After: true
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = GreaterThanOrEqual(parameter, Constant(int.MinValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesGreaterThanMax()
    {
        // Before: x > int.MaxValue
        // After: false
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = GreaterThan(parameter, Constant(int.MaxValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesLessThanOrEqualMax()
    {
        // Before: x <= int.MaxValue
        // After: true
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = LessThanOrEqual(parameter, Constant(int.MaxValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesUnsignedLessThanMin()
    {
        // Before: x < 0u
        // After: false
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = LessThan(parameter, Constant(0u));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesUnsignedMaxLessThan()
    {
        // Before: uint.MaxValue < x
        // After: false
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = LessThan(Constant(uint.MaxValue), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesUnsignedLessThanOrEqualMax()
    {
        // Before: x <= uint.MaxValue
        // After: true
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = LessThanOrEqual(parameter, Constant(uint.MaxValue));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesUnsignedGreaterThanOrEqualMin()
    {
        // Before: x >= 0u
        // After: true
        ParameterExpression parameter = Parameter(typeof(uint), "x");
        BinaryExpression expression = GreaterThanOrEqual(parameter, Constant(0u));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesZeroMinusX()
    {
        // Before: 0 - x
        // After: -x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Subtract(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesZeroMinusXChecked()
    {
        // Before: checked(0 - x)
        // After: checked(-x)
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = SubtractChecked(Constant(0), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesAllOnesMinusX()
    {
        // Before: -1 - x
        // After: ~x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Subtract(Constant(-1), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMultiplyByMinusOne()
    {
        // Before: x * -1
        // After: -x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(parameter, Constant(-1));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesMultiplyByMinusOneLeft()
    {
        // Before: -1 * x
        // After: -x
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(Constant(-1), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolEqualsTrue()
    {
        // Before: b == true
        // After: b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = Equal(parameter, Constant(true));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolEqualsFalse()
    {
        // Before: b == false
        // After: !b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = Equal(parameter, Constant(false));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolNotEqualsTrue()
    {
        // Before: b != true
        // After: !b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = NotEqual(parameter, Constant(true));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolNotEqualsFalse()
    {
        // Before: b != false
        // After: b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = NotEqual(parameter, Constant(false));

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolEqualsTrueLeftConstant()
    {
        // Before: true == b
        // After: b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = Equal(Constant(true), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolEqualsFalseLeftConstant()
    {
        // Before: false == b
        // After: !b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = Equal(Constant(false), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolNotEqualsFalseLeftConstant()
    {
        // Before: false != b
        // After: b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = NotEqual(Constant(false), parameter);

        await OptimizeAsync(expression);
    }

    [Fact]
    public async Task SimplifiesBoolNotEqualsTrueLeftConstant()
    {
        // Before: true != b
        // After: !b
        ParameterExpression parameter = Parameter(typeof(bool), "b");
        BinaryExpression expression = NotEqual(Constant(true), parameter);

        await OptimizeAsync(expression);
    }

    [Theory]
    [MemberData(nameof(GetSemiComplexExpressions))]
    public async Task OptimizesSemiComplexExpressions(string name, Expression expression)
    {
        Expression optimized = ExpressionOptimizer.Visit(expression);
        await VerifyOptimized(optimized, nameof(OptimizesSemiComplexExpressions) + "_" + name);
    }

    public static TheoryData<string, Expression> GetSemiComplexExpressions()
    {
        ParameterExpression x = Parameter(typeof(int), "x");
        ParameterExpression y = Parameter(typeof(int), "y");
        ParameterExpression b = Parameter(typeof(bool), "b");

        return new TheoryData<string, Expression>
        {
            // (x + 0) * 1 + (2 + 3) -> (x + 5)
            { "ArithmeticChain", Add(Multiply(Add(x, Constant(0)), Constant(1)), Add(Constant(2), Constant(3))) },
            // (x > 3) && (x > 5) ? true : false -> (x > 5)
            { "RangeConditional", Condition(AndAlso(GreaterThan(x, Constant(3)), GreaterThan(x, Constant(5))), Constant(true), Constant(false)) },
            // (x ^ x) | (y & 0) -> 0
            { "BitwiseMix", Or(ExclusiveOr(x, x), And(y, Constant(0))) },
            // (x << 0) + (0 >> y) -> x
            { "ShiftMix", Add(LeftShift(x, Constant(0)), RightShift(Constant(0), y)) },
            // (b && true) || false -> b
            { "BooleanMix", OrElse(AndAlso(b, Constant(true)), Constant(false)) }
        };
    }

    private static async Task OptimizeAsync(Expression exp, [CallerMemberName]string testName = "")
    {
        Expression optimized = ExpressionOptimizer.Visit(exp);
        await VerifyOptimized(optimized, testName);
    }

    private static Task VerifyOptimized(Expression optimized, string testName)
    {
        return Verify(optimized.ToString())
               .UseDirectory("Verify/ExpressionOptimizer")
               .UseFileName(testName)
               .DisableDiff();
    }

    private sealed class BoolHolder
    {
        public bool Value { get; set; }
    }
}
