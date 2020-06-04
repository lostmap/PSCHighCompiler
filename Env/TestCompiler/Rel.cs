﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Rel : Logical
    {
        public Rel(Token tok, Expr x1, Expr x2) : base(tok, x1, x2) { }

        public new Type check(Type p1, Type p2)
        {
            if (p1 is Array || p2 is Array) return null;
            else if (p1 == p2) return Type.Bool;
            else return null;
        }

        public override void jumping(int t, int f)
        {
            Expr a = expr1.reduce();
            Expr b = expr2.reduce();
            String test = a.ToString() + " " + op.ToString() + " " + b.ToString();
            emitjumps(test, t, f);
        }
    }
}
