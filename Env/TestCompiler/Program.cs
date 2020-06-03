using System;
using System.IO;

namespace TestCompiler
{
    /*class Parser
    {
        static int lookahead;

        public Parser()
        {
            lookahead = Console.Read();
        }
        public void Expr()
        {
            Term();
            while(true)
            {
                if (lookahead == '+')
                {
                    Match('+');
                    Term();
                    Console.Write('+');
                }
                else if (lookahead == '-')
                {
                    Match('-');
                    Term();
                    Console.Write('-');
                }
                else
                {
                    return;
                }
            }
        }
        public void Term()
        {
            if (Char.IsDigit((char)lookahead))
            {
                Console.Write((char)lookahead);
                Match(lookahead);
            }
            else
            {
                throw new Exception("Syntax error");
            }
        }

        public void Match(int term)
        {
            if (lookahead == term)
            {
                lookahead = Console.Read();
            } 
            else
            {
                throw new Exception("Syntax error");
            }
        }
    }
    */
    public class Program
    {
        public static void Main(string[] args)
        {
            Lexer lex = new Lexer();
            Console.Write('\n');
        }
    }
}
