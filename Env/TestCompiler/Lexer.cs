using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace TestCompiler
{

    public class Lexer
    {
        public static int line = 1;
        char peek = ' ';
        Hashtable words = new Hashtable();
        void reserve(Word w)
        {
            words.Add(w.lexeme, w);
        }

        public Lexer()
        {

            reserve(new Word("if", Tag.IF));
            reserve(new Word("else", Tag.ELSE));
            reserve(new Word("while", Tag.WHILE));
            reserve(new Word("do", Tag.DO));
            reserve(new Word("break", Tag.BREAK));

            reserve(Word.True); reserve(Word.False);

            reserve(Type.Int); reserve(Type.Char);
            reserve(Type.Bool); reserve(Type.Float);
        }

        void readch() 
        {
            peek = (char) Console.Read();
        }
        bool readch(char c)
        {
            readch();
            if( peek != c ) return false;
            peek = ' ';
            return true;
        }
        public Token scan()
        {
            for (; ; readch())
            {
                if (peek == ' ' || peek == '\t') continue;
                else if (peek == '\n') line = line + 1;
                else break;
            }
            switch( peek )
            {
                case '&':
                    if (readch('&')) return Word.and; else return new Token('&');
                case '|':
                     if (readch('|')) return Word.or; else return new Token('|');
                case '=':
                     if (readch('=')) return Word.eq; else return new Token('=');
                case '!':
                    if (readch('=')) return Word.ne; else return new Token('!');
                case '<':
                    if (readch('=')) return Word.le; else return new Token('<');
                case '>':
                    if (readch('=')) return Word.ge; else return new Token('>');
            }
            if (Char.IsDigit(peek))
            {
                int v = 0;
                do
                {
                    v = 10 * v + Convert.ToInt32(peek.ToString(), 10);
                    readch();
                } while (Char.IsDigit(peek));

                if (peek != '.') return new Num(v);

                float x = v; float d = 10;
                for (; ; )
                {
                    readch();
                    if (!Char.IsDigit(peek)) break;
                    x = x + Convert.ToInt32(peek.ToString(), 10) / d;
                    d = d * 10;
                }
                return new Real(x);
            }
            if (Char.IsLetter(peek)) {
                StringBuilder b = new StringBuilder();
                do
                {
                    b.Append(peek);
                    readch();
                } while (Char.IsLetterOrDigit(peek));
                String s = b.ToString();
                Word w = (Word) words[s];
                if (w != null) return w;
                w = new Word(s, Tag.ID);
                words.Add(s, w);
                return w;
            }
            Token tok = new Token(peek); peek = ' ';
            return tok;
        }
    }
}
