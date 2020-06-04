﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Unary : Op
    {
        public Expr expr;

        public Unary(Token tok, Expr x) : base(tok, null)
        {    // handles minus, for ! see Not
            expr = x;
            type = Type.max(Type.Int, expr.type);
            if (type == null) error("type error");
        }

        public Expr gen() { return new Unary(op, expr.reduce()); }

        public String toString() { return op.toString() + " " + expr.toString(); }
    }
}
