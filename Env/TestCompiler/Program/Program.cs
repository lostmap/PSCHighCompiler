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
            
            //string code = File.ReadAllText("C:/Users/Oleg/Downloads/dragon-book-source-code-master/dragon-book-source-code-master/tests/test.i");
            string code = "{int a; int b; a = 0; b = 0;{ int b; b = 1; { int a; a = 2; } { int b; b = 3; } a = a + 1; b = b + 1; } a = a + 1; b = b + 1; }";
            code = "{ int i;  double prod; double [20] a; double [20] b; prod = 0; i = 1; do { prod = prod + a[i]*b[i]; i = i+1; } while (i <= 20); }";
            code = "{ bool b; bool r; bool[11] a; int i; int x; int y; r = b; r = a[i]; a[i] = b; a[i] = true; a[i] = false; if (b) x = y; if (a[i]) x = y; }";
            code = "{ int a; a = true; }";
            Lexer lex = new Lexer(code);
            Parser parse = new Parser(lex);
            parse.program();
            Console.Write('\n');
        }
    }
}
