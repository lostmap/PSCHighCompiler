using System;
using System.Collections.Generic;
using System.Text;

namespace TestCompiler
{
    public class Node
    {

        int lexline = 0;

        public Node() { lexline = Lexer.line; }

        public void error(String s) { throw new Exception("near line " + lexline + ": " + s); }

        static int labels = 0;

        public int newlabel() { return ++labels; }

        public void emitlabel(int i) { Console.Write("L" + i + ":"); }

        public void emit(String s) { Console.WriteLine("\t" + s); }
    }
}
