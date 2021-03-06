﻿namespace inter
{
    using lexer;
    using symbols;

    public class Arith : Op
    {
        public Expr expr1, expr2;

        public Arith(Token tok, Expr x1, Expr x2) : base(tok, null)
        {
            expr1 = x1; expr2 = x2;
            type = Type.max(expr1.type, expr2.type);
            if (type == null)
            {
                error("type error");
            }
        }

        public override Expr gen()
        {
            return new Arith(op, expr1.reduce(), expr2.reduce());
        }

        public override string ToString()
        {
            return expr1.ToString() + " " + op.ToString() + " " + expr2.ToString();
        }
    }
}
