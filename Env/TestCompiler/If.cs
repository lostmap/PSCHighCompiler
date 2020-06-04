using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class If : Stmt
    {
        Expr expr; Stmt stmt;

        public If(Expr x, Stmt s)
        {
            expr = x; stmt = s;
            if (expr.type != Type.Bool) expr.error("boolean required in if");
        }

        public override void gen(int b, int a)
        {
            int label = newlabel(); // label for the code for stmt
            expr.jumping(0, a);     // fall through on true, goto a on false
            emitlabel(label); stmt.gen(label, a);
        }
    }
}
