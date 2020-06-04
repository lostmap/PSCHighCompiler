using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Stmt : Node
    {
        public Stmt() { }

        public static Stmt Null = new Stmt();

        public void gen(int b, int a) { } // called with labels begin and after

        public int after = 0;                   // saves label after
        public static Stmt Enclosing = Stmt.Null;  // used for break stmts
    }
}
