using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Op : Expr
    {
        public Op(Token tok, Type p) : base(tok, p) {}

        public override Expr reduce()
        {
            Expr x = gen();
            Temp t = new Temp(type);
            emit(t.ToString() + " = " + x.ToString());
            return t;
        }
    }
}
