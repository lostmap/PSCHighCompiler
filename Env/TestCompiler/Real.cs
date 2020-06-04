using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Real : Token
    {
        public float value;

        public Real(float v) : base(Tag.REAL)
        {
            value = v;
        }

        public override String ToString()
        {
            return "" + value;
        }
    }
}
