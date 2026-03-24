namespace Genbox.FastData.Tests;

public sealed class ExpressionOptimizerManualTests(ITestOutputHelper output)
{
    [Fact]
    public void BooleanAlgebraEquivalencesHold()
    {
        static bool Or(bool a, bool b) => a || b;
        static bool And(bool a, bool b) => a && b;
        static bool Not(bool a) => !a;

        foreach (bool u in new[] { true, false })
            foreach (bool p in new[] { true, false })
                foreach (bool p1 in new[] { true, false })
                    foreach (bool p2 in new[] { true, false })
                        foreach (bool p3 in new[] { true, false })
                            foreach (bool p4 in new[] { true, false })
                                foreach (bool p5 in new[] { true, false })
                                {
                                    bool distribute = And(p, Or(p1, p2));
                                    bool replacement = (p && p1) || (p && p2);
                                    Assert.Equal(distribute, replacement);

                                    bool gather = And(Or(p, p1), Or(p2, p3));
                                    if (p == p2)
                                    {
                                        bool replacement2 = p || (p1 && p3);
                                        Assert.Equal(gather, replacement2);
                                    }

                                    bool absorb = And(Or(p1, u), p);
                                    if (p == p1)
                                    {
                                        bool replacement3 = p || (p1 && p3);
                                        Assert.Equal(absorb, replacement3);
                                    }

                                    foreach (bool u2 in new[] { true, false })
                                    {
                                        if (p == p1)
                                        {
                                            bool pRep1a = And(Or(u, Or(p, u2)), p1);
                                            Assert.Equal(p, pRep1a);

                                            bool pRep1b = And(Or(Or(p, u2), u), p1);
                                            Assert.Equal(p, pRep1b);

                                            bool pRep1c = And(Or(u, Or(u2, p)), p1);
                                            Assert.Equal(p, pRep1c);

                                            bool pRep1d = And(Or(Or(u2, p), u), p1);
                                            Assert.Equal(p, pRep1d);

                                            bool pRep1e = And(p1, Or(Or(p, u2), u));
                                            Assert.Equal(p, pRep1e);

                                            bool pRep1f = And(p1, Or(u, Or(p, u2)));
                                            Assert.Equal(p, pRep1f);

                                            bool pRep1g = And(p1, Or(Or(u2, p), u));
                                            Assert.Equal(p, pRep1g);

                                            bool pRep1h = And(p1, Or(u, Or(u2, p)));
                                            Assert.Equal(p, pRep1h);

                                            bool pRep2a = And(p, And(p2, Or(p1, u)));
                                            Assert.Equal(p && p2, pRep2a);

                                            bool pRep2b = And(p, And(p2, Or(u, p1)));
                                            Assert.Equal(p && p2, pRep2b);

                                            bool pRep2c = And(And(p2, Or(p1, u)), p);
                                            Assert.Equal(p && p2, pRep2c);

                                            bool pRep2d = And(And(p2, Or(u, p1)), p);
                                            Assert.Equal(p && p2, pRep2d);

                                            bool pRep2e = And(p, And(Or(p1, u), p2));
                                            Assert.Equal(p && p2, pRep2e);

                                            bool pRep2f = And(p, And(Or(u, p1), p2));
                                            Assert.Equal(p && p2, pRep2f);

                                            bool pRep2g = And(And(Or(p1, u), p2), p);
                                            Assert.Equal(p && p2, pRep2g);

                                            bool pRep2h = And(And(Or(u, p1), p2), p);
                                            Assert.Equal(p && p2, pRep2h);

                                            bool pRep3a = Or(p, Or(p2, And(p1, u)));
                                            Assert.Equal(p || p2, pRep3a);

                                            bool pRep3b = Or(p, Or(p2, And(u, p1)));
                                            Assert.Equal(p || p2, pRep3b);

                                            bool pRep3c = Or(p, Or(And(p1, u), p2));
                                            Assert.Equal(p || p2, pRep3c);

                                            bool pRep3d = Or(p, Or(And(u, p1), p2));
                                            Assert.Equal(p || p2, pRep3d);

                                            bool pRep3e = Or(Or(And(p1, u), p2), p);
                                            Assert.Equal(p || p2, pRep3e);

                                            bool pRep3f = Or(Or(And(u, p1), p2), p);
                                            Assert.Equal(p || p2, pRep3f);

                                            bool pRep3g = Or(Or(p2, And(p1, u)), p);
                                            Assert.Equal(p || p2, pRep3g);

                                            bool pRep3h = Or(Or(p2, And(u, p1)), p);
                                            Assert.Equal(p || p2, pRep3h);

                                            bool pRep3i = Or(Or(p, p2), And(p1, u));
                                            Assert.Equal(p || p2, pRep3i);

                                            bool pRep3j = Or(Or(p, p2), And(u, p1));
                                            Assert.Equal(p || p2, pRep3j);

                                            bool pRep3k = Or(And(p1, u), Or(p, p2));
                                            Assert.Equal(p || p2, pRep3k);

                                            bool pRep3l = Or(And(p1, u), Or(p2, p));
                                            Assert.Equal(p || p2, pRep3l);

                                            bool pRep3m = Or(And(u, p1), Or(p, p2));
                                            Assert.Equal(p || p2, pRep3m);

                                            bool pRep3n = Or(And(u, p1), Or(p2, p));
                                            Assert.Equal(p || p2, pRep3n);

                                            bool pRep3o = Or(Or(p2, p), And(p1, u));
                                            Assert.Equal(p || p2, pRep3o);

                                            bool pRep3p = Or(Or(p2, p), And(u, p1));
                                            Assert.Equal(p || p2, pRep3p);
                                        }
                                    }

                                    if (p == p1)
                                    {
                                        bool test1a = And(p, Or(Not(p1), p2));
                                        Assert.Equal(p && p2, test1a);

                                        bool test1b = And(p, Or(p2, Not(p1)));
                                        Assert.Equal(p && p2, test1b);

                                        bool test1c = And(Or(p2, Not(p1)), p);
                                        Assert.Equal(p && p2, test1c);
                                    }

                                    if (p == p1)
                                    {
                                        bool test2a = Or(p, And(Not(p1), p2));
                                        Assert.Equal(p || p2, test2a);

                                        bool test2b = Or(And(p2, Not(p1)), p);
                                        Assert.Equal(p || p2, test2b);
                                    }

                                    if (p == p1 && p2 == p4)
                                    {
                                        bool testTrue1 = Or(Or(p2, Not(p)), And(p1, Not(p4)));
                                        Assert.True(testTrue1);

                                        bool testTrue2 = Or(Or(Not(p), p2), And(p1, Not(p4)));
                                        Assert.True(testTrue2);

                                        bool testTrue3 = Or(Or(Not(p), p2), And(Not(p4), p1));
                                        Assert.True(testTrue3);
                                    }

                                    if (p == p1 && p2 == p4)
                                    {
                                        bool testNotP2a = Or(Not(Or(p, p2)), And(p1, Not(p4)));
                                        Assert.Equal(!p2, testNotP2a);

                                        bool testNotP2b = Or(Not(Or(p2, p)), And(p1, Not(p4)));
                                        Assert.Equal(!p2, testNotP2b);

                                        bool testNotP2c = Or(And(p1, Not(p4)), Not(Or(p, p2)));
                                        Assert.Equal(!p2, testNotP2c);

                                        bool testNotP2d = Or(And(p1, Not(p4)), Not(Or(p2, p)));
                                        Assert.Equal(!p2, testNotP2d);
                                    }

                                    if (p == p1)
                                    {
                                        foreach (bool u2 in new[] { true, false })
                                        {
                                            Assert.True(Or(Or(p, u), Or(Not(p1), u2)));
                                            Assert.True(Or(Or(u, p), Or(Not(p1), u2)));
                                            Assert.True(Or(Or(p, u), Or(u2, Not(p1))));
                                            Assert.True(Or(Or(u, p), Or(u2, Not(p1))));
                                            Assert.True(Or(Or(Not(p1), u2), Or(p, u)));
                                            Assert.True(Or(Or(Not(p1), u2), Or(u, p)));
                                            Assert.True(Or(Or(u2, Not(p1)), Or(p, u)));
                                            Assert.True(Or(Or(u2, Not(p1)), Or(u, p)));
                                        }
                                    }

                                    if (p == p1)
                                    {
                                        bool repl1 = Or(Or(Not(p3), p), And(p4, Not(p1)));
                                        Assert.Equal(p || p4 || !p3, repl1);

                                        bool repl2 = Or(Or(p, Not(p3)), And(p4, Not(p1)));
                                        Assert.Equal(p || p4 || !p3, repl2);

                                        bool repl3 = Or(Or(Not(p3), p), And(Not(p1), p4));
                                        Assert.Equal(p || p4 || !p3, repl3);

                                        bool repl4 = Or(Or(p, Not(p3)), And(Not(p1), p4));
                                        Assert.Equal(p || p4 || !p3, repl4);

                                        bool repl5 = Or(And(p4, Not(p1)), Or(Not(p3), p));
                                        Assert.Equal(p || p4 || !p3, repl5);

                                        bool repl6 = Or(And(p4, Not(p1)), Or(p, Not(p3)));
                                        Assert.Equal(p || p4 || !p3, repl6);

                                        bool repl7 = Or(And(Not(p1), p4), Or(Not(p3), p));
                                        Assert.Equal(p || p4 || !p3, repl7);

                                        bool repl8 = Or(And(Not(p1), p4), Or(p, Not(p3)));
                                        Assert.Equal(p || p4 || !p3, repl8);
                                    }

                                    if (p == p1)
                                    {
                                        bool repl2a = Or(Not(Or(p, p3)), And(p4, Not(p1)));
                                        Assert.Equal(!p && (!p3 || p4), repl2a);

                                        bool repl2b = Or(Not(Or(p, p3)), And(Not(p1), p4));
                                        Assert.Equal(!p && (!p3 || p4), repl2b);

                                        bool repl2c = Or(And(Not(p1), p4), Not(Or(p3, p)));
                                        Assert.Equal(!p && (!p3 || p4), repl2c);

                                        bool repl2d = Or(And(Not(p1), p4), Not(Or(p, p3)));
                                        Assert.Equal(!p && (!p3 || p4), repl2d);

                                        bool repl2e = Or(Not(Or(p3, p)), And(Not(p1), p4));
                                        Assert.Equal(!p && (!p3 || p4), repl2e);

                                        bool repl2f = Or(Not(Or(p3, p)), And(p4, Not(p1)));
                                        Assert.Equal(!p && (!p3 || p4), repl2f);

                                        bool repl2g = Or(And(p4, Not(p1)), Not(Or(p3, p)));
                                        Assert.Equal(!p && (!p3 || p4), repl2g);

                                        bool repl2h = Or(And(p4, Not(p1)), Not(Or(p, p3)));
                                        Assert.Equal(!p && (!p3 || p4), repl2h);
                                    }

                                    if (p == p1)
                                    {
                                        bool repl3a = Or(Or(p, p2), Not(Or(p3, p1)));
                                        Assert.Equal(p || p2 || !p3, repl3a);

                                        bool repl3b = Or(Or(p, p2), Not(Or(p1, p3)));
                                        Assert.Equal(p || p2 || !p3, repl3b);

                                        bool repl3c = Or(Not(Or(p1, p3)), Or(p, p2));
                                        Assert.Equal(p || p2 || !p3, repl3c);

                                        bool repl3d = Or(Not(Or(p1, p3)), Or(p2, p));
                                        Assert.Equal(p || p2 || !p3, repl3d);

                                        bool repl3e = Or(Or(p2, p), Not(Or(p3, p1)));
                                        Assert.Equal(p || p2 || !p3, repl3e);

                                        bool repl3f = Or(Or(p2, p), Not(Or(p1, p3)));
                                        Assert.Equal(p || p2 || !p3, repl3f);

                                        bool repl3g = Or(Not(Or(p3, p1)), Or(p, p2));
                                        Assert.Equal(p || p2 || !p3, repl3g);

                                        bool repl3h = Or(Not(Or(p3, p1)), Or(p2, p));
                                        Assert.Equal(p || p2 || !p3, repl3h);
                                    }
                                }
    }
}