using System;
using System.Text;
using System.Collections;

namespace lexer
{
    using symbols;

    public class Lexer
    {
        public static int line = 1;
        internal char peek = ' ';
        internal string code = "";
        internal int counter = 0;
        internal Hashtable words = new Hashtable();
        /*
        public Lexer(string _code = "")
        {
            code = _code;
        } */

        void reserve(Word w)
        {
            words[w.lexeme] = w;
        }

        public Lexer(string _code = "")
        {
            code = _code;
            reserve(new Word("if", Tag.IF));
            reserve(new Word("else", Tag.ELSE));
            reserve(new Word("while", Tag.WHILE));
            reserve(new Word("do", Tag.DO));
            reserve(new Word("break", Tag.BREAK));
            reserve(Word.True); 
            reserve(Word.False);
            reserve(Type.Int); 
            reserve(Type.Char);
            reserve(Type.Bool); 
            reserve(Type.Float);
        }

        internal virtual void readch() 
        {
            //if (counter < code.Length)
            //{
                peek = code[counter++];
            //}
            //peek = (char)Console.Read();
        }

        internal virtual bool readch(char c)
        {
            readch();
            if (peek != c)
            {
                --counter;
                return false;
            }
            peek = ' ';
            return true;
        }

        public virtual Token scan()
        {
            for (; ; readch())
            {
                if (counter == code.Length)
                {
                    break;
                }
                else if (peek == ' ' || peek == '\t' || peek == '\r')
                {
                    continue;
                }
                else if (peek == '\n')
                {
                    line = line + 1;
                }
                else
                {
                    break;
                }
            }
            switch( peek )
            {
                case '&':
                    if (readch('&')) { return Word.and; } else { return new Token('&'); }
                case '|':
                    if (readch('|')) { return Word.or; } else { return new Token('|'); }
                case '=':
                    if (readch('=')) { return Word.eq; } else { return new Token('='); }
                case '!':
                    if (readch('=')) { return Word.ne; } else { return new Token('!'); }
                case '<':
                    if (readch('=')) { return Word.le; } else { return new Token('<'); }
                case '>':
                    if (readch('=')) { return Word.ge; } else { return new Token('>'); }
            }
            if (Char.IsDigit(peek))
            {
                int v = 0;
                do
                {
                    v = 10 * v + Convert.ToInt32(peek.ToString(), 10);
                    readch();
                } while (Char.IsDigit(peek));

                if (peek != '.')
                {
                    return new Num(v);
                }

                float x = v; float d = 10;
                for (;;)
                {
                    readch();
                    if (!Char.IsDigit(peek))
                    { 
                        break; 
                    }
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
                if (w != null)
                {
                    return w;
                }
                w = new Word(s, Tag.ID);
                words.Add(s, w);
                return w;
            }
            Token tok = new Token(peek); peek = ' ';
            return tok;
        }
    }
}
