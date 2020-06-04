using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Num : Token
    {
        public int value;

        public Num(int v) : base(Tag.NUM)
        { 
            value = v;
        }

        public override String ToString() 
        { 
            return "" + value; 
        }
    }
}
