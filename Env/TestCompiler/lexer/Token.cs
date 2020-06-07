namespace lexer
{
    public class Token
    {
        public readonly int tag;

        public Token(int t)
        {
            tag = t; 
        }

        public override string ToString() 
        {
            return "" + (char)tag;
        }
    }
}
