namespace lexer
{
    public class Real : Token
    {
        public readonly double value;

        public Real(double v) : base(Tag.REAL)
        {
            value = v;
        }

        public override string ToString()
        {
            return "" + value;
        }
    }
}
