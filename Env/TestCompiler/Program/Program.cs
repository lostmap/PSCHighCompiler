using System;
using System.IO;

namespace Program
{
    using lexer;
    using parser;
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
            string code = File.ReadAllText("C:/Users/Oleg/Downloads/dragon-book-source-code-master/dragon-book-source-code-master/tests/test.i");
            Lexer lex = new Lexer(code);
            Parser parse = new Parser(lex);
            parse.program();
            Console.Write('\n');
        }
    }
}
