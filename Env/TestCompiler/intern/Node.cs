using System;

namespace inter
{
    using lexer;

    public class Node
    {

        internal int lexline = 0;

        internal Node() 
        {
            lexline = Lexer.line; 
        }

        internal virtual void error(String s)
        {
            throw new Exception("near line " + lexline + ": " + s);
        }

        internal static int labels = 0;

        public virtual int newlabel()
        {
            return ++labels;
        }

        public virtual void emitlabel(int i)
        {
            Console.Write("L" + i + ":");
        }

        public virtual void emit(string s)
        {
            Console.WriteLine("\t" + s);
        }
    }
}
