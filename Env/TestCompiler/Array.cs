using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Array : Type
    {
        public Type of;                  // array *of* type
        public int size = 1;             // number of elements
        public Array(int sz, Type p) : base ("[]", Tag.INDEX, sz * p.width)
        {
            size = sz; of = p;
        }
        public String toString() { return "[" + size + "] " + of.toString(); }
    }
}
