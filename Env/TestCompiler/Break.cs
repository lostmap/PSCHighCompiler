using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Break : Stmt
    {
        Stmt stmt;
        public Break()
        {
            if (Stmt.Enclosing == Stmt.Null) error("unenclosed break");
            stmt = Stmt.Enclosing;
        }

        public override void gen(int b, int a)
        {
            emit("goto L" + stmt.after);
        }
    }
}
