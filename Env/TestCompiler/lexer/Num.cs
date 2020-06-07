namespace lexer
{
    public class Num : Token
    {
        public readonly int value;

        public Num(int v) : base(Tag.NUM)
        { 
            value = v;
        }

        public override string ToString() 
        { 
            return "" + value; 
        }
    }
}
