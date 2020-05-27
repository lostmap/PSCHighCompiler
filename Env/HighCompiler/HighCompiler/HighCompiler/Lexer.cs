using System;

namespace PSCompiler
{
    public enum Token : ushort
    {
        NUM,
        VAR,
        NAME,
        SET,
        SUM,
        SUB,
        GREATER,
        LESS,
        GREATEREQUAL,
        LESSEQUAL,
        EQUAL,
        NOTEQUAL,
        IF,
        ELSE,
        OR,
        AND,
        NOT,
        WHILE,
        DO,
        FOR,
        TO,
        LBRA,
        RBRA,
        LPAR,
        RPAR,
        SEMICOLON,
        NONE,
        MULT,
        DIV,
    }

    class Lexer
    {
        private readonly string code;
        private int counter;

        private Token token;
        private string value;

        public Lexer(string _code = "")
        {
            this.code = _code;
            this.counter = 0;
        }

        private bool IsSeparator(char c)
        {
            return (c == '\n' || c == '\r' || c == '\t' || c == ' ');
        }

        public void DetermineNextToken()
        {
            token = Token.NONE;
            value = "";

            for (; token == Token.NONE;)
            {
                if (counter == code.Length)
                {
                    return;
                }
                if (IsSeparator(code[counter]))
                {
                    ++counter;
                    continue;
                }
                else if (Char.IsDigit(code[counter]))
                {
                    token = Token.NUM;

                    while (Char.IsDigit(code[counter]))
                    {
                        value += code[counter++];
                    }

                    if (code[counter] == '.')
                    {
                        value += code[counter++];

                        while (Char.IsDigit(code[counter]))
                        {
                            value += code[counter++];
                        }
                    }
                }
                else if (Char.IsLetter(code[counter]))
                {
                    string word = "";

                    while (Char.IsLetterOrDigit(code[counter]))
                    {
                        word += code[counter++];
                    }

                    switch (word)
                    {
                        case "var":
                            token = Token.VAR;
                            break;
                        case "if":
                            token = Token.IF;
                            break;
                        case "else":
                            token = Token.ELSE;
                            break;
                        case "while":
                            token = Token.WHILE;
                            break;
                        case "do":
                            token = Token.WHILE;
                            break;
                        case "for":
                            token = Token.FOR;
                            break;
                        case "to":
                            token = Token.TO;
                            break;
                        default:
                            token = Token.NAME;
                            value = word;
                            break;
                    }
                }
                else
                {
                    switch (code[counter++])
                    {
                        case '+':
                            token = Token.SUM;
                            break;
                        case '-':
                            token = Token.SUB;
                            break;
                        case '*':
                            token = Token.MULT;
                            break;
                        case '/':
                            token = Token.DIV;
                            break;
                        case '=':
                            if (code[counter] == '=')
                            {
                                token = Token.EQUAL;
                                ++counter;
                            }
                            else
                            {
                                token = Token.SET;
                            }
                            break;
                        case '>':
                            if (code[counter] == '=')
                            {
                                token = Token.GREATEREQUAL;
                                ++counter;
                            }
                            else
                            {
                                token = Token.GREATER;
                            }
                            break;
                        case '<':
                            if (code[counter] == '=')
                            {
                                token = Token.LESSEQUAL;
                                ++counter;
                            }
                            else
                            {
                                token = Token.LESS;
                            }
                            break;
                        case '!':
                            if (code[counter] == '=')
                            {
                                token = Token.NOTEQUAL;
                                ++counter;
                            }
                            else
                            {
                                token = Token.NOT;
                            }
                            break;
                        case '&':
                            token = Token.AND;
                            break;
                        case '|':
                            token = Token.OR;
                            break;
                        case ';':
                            token = Token.SEMICOLON;
                            break;
                        case '{':
                            token = Token.LBRA;
                            break;
                        case '}':
                            token = Token.RBRA;
                            break;
                        case '(':
                            token = Token.LPAR;
                            break;
                        case ')':
                            token = Token.RPAR;
                            break;
                        default:
                            throw new Exception("unexpected symbol");
                    }
                }
            }
        }

        public Token GetToken()
        {
            return this.token;
        }
        public string GetValue()
        {
            return this.value;
        }
    }
}
