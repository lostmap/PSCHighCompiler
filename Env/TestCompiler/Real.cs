using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    class Real : Token
    {
        public float value;

        public Real(float v) : base(Tag.REAL)
        {
            value = v;
        }

        public String toString()
        {
            return "" + value;
        }
    }
}
