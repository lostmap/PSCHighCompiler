using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class SetElem : Stmt
    {
        public Id array; public Expr index; public Expr expr;

        public SetElem(Access x, Expr y)
        {
            array = x.array; index = x.index; expr = y;
            if (check(x.type, expr.type) == null) error("type error");
        }

        public Type check(Type p1, Type p2)
        {
            if (p1 is Array || p2 is Array ) return null;
            else if (p1 == p2) return p2;
            else if (Type.numeric(p1) && Type.numeric(p2)) return p2;
            else return null;
        }

        public override void gen(int b, int a)
        {
            String s1 = index.reduce().ToString();
            String s2 = expr.reduce().ToString();
            emit(array.ToString() + " [ " + s1 + " ] = " + s2);
        }
    }
}
