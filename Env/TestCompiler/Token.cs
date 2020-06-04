using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Token
    {
        public int tag;

        public Token(int t)
        {
            tag = t; 
        }

        public String toString() 
        {
            return "" + (char)tag;
        }
    }
}
