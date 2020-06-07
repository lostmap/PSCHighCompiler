namespace inter
{
    using lexer;
    using symbols;

    public class Temp : Expr
    {
        internal static int count = 0;
        internal int number = 0;

        public Temp(Type p) : base(Word.temp, p)
        { 
            number = ++count; 
        }

        public override string ToString()
        {
            return "t" + number; 
        }
    }
}
