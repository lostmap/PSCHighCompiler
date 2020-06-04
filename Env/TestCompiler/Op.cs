using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Op : Expr
    {
        public Op(Token tok, Type p) : base(tok, p) {}

        public Expr reduce()
        {
            Expr x = gen();
            Temp t = new Temp(type);
            emit(t.toString() + " = " + x.toString());
            return t;
        }
    }
}
