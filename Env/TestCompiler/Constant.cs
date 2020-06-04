using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Constant : Expr
    {
        public Constant(Token tok, Type p) : base(tok, p) { }
        public Constant(int i) : base(new Num(i), Type.Int) { }

        public static Constant
        True  = new Constant(Word.True, Type.Bool),
        False = new Constant(Word.False, Type.Bool);

        public void jumping(int t, int f)
        {
            if (this == True && t != 0) emit("goto L" + t);
            else if (this == False && f != 0) emit("goto L" + f);
        }
    }
}
