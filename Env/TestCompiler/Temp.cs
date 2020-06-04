using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Temp : Expr
    {
        static int count = 0;
        int number = 0;

        public Temp(Type p) : base(Word.temp, p) { number = ++count; }

        public String toString() { return "t" + number; }
    }
}
