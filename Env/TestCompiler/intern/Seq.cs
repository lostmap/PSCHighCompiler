using symbols;
using System;

namespace inter
{
    public class Seq : Stmt
    {
        internal Stmt stmt1;
        internal Stmt stmt2;

        public Seq(Stmt s1, Stmt s2)
        {
            stmt1 = s1;
            stmt2 = s2;
        }

        public override void gen(int b, int a)
        {
            if (stmt1 == Stmt.Null)
            {
                stmt2.gen(b, a);
            }
            else if (stmt2 == Stmt.Null)
            {
                stmt1.gen(b, a);
            }
            else
            {
                int label = newlabel();
                stmt1.gen(b, label);
                emitlabel(label);
                stmt2.gen(label, a);
            }
        }

        public override void bytecode(Env currEnv)
        {
            if (stmt1 == Stmt.Null)
            {
                stmt2.bytecode(currEnv);
            }
            else if (stmt2 == Stmt.Null)
            {
                stmt1.bytecode(currEnv);
            }
            else
            {
                //Console.Write("todo 2 stmt");
            }
        }
    }
}
