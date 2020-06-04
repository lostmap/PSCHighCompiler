using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Logical : Expr
    {
        public Expr expr1, expr2;

        public Logical(Token tok, Expr x1, Expr x2) : base(tok, null)  // null type to start
        {         
            expr1 = x1; expr2 = x2;
            type = check(expr1.type, expr2.type);
            if (type == null) error("type error");
        }

        public Type check(Type p1, Type p2)
        {
            if (p1 == Type.Bool && p2 == Type.Bool) return Type.Bool;
            else return null;
        }

        public override Expr gen()
        {
            int f = newlabel(); int a = newlabel();
            Temp temp = new Temp(type);
            this.jumping(0, f);
            emit(temp.ToString() + " = true");
            emit("goto L" + a);
            emitlabel(f); emit(temp.ToString() + " = false");
            emitlabel(a);
            return temp;
        }

        public override String ToString()
        {
            return expr1.ToString() + " " + op.ToString() + " " + expr2.ToString();
        }
    }
}
