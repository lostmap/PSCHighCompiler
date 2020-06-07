namespace lexer
{
    public class Real : Token
    {
        public readonly float value;

        public Real(float v) : base(Tag.REAL)
        {
            value = v;
        }

        public override string ToString()
        {
            return "" + value;
        }
    }
}
