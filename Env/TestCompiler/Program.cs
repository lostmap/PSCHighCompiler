using System;
using System.IO;

namespace TestCompiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Lexer lex = new Lexer();
            Parser parse = new Parser(lex);
            parse.program();
            Console.Write('\n');
        }
    }
}
