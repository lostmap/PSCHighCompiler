namespace symbols
{
    using lexer;

    public class Type : Word
    {
        public int width = 0;          // width is used for storage allocation

        public Type(string s, int tag, int w) : base(s, tag)
        { 
            width = w;
        }

        public static readonly Type
        //Var   = new Type("var", Tag.BASIC, 1),
        Int   = new Type("int", Tag.BASIC, 4),
        Double = new Type("double", Tag.BASIC, 8),
        Char  = new Type("char", Tag.BASIC, 1),
        Bool  = new Type("bool", Tag.BASIC, 1);

        public static bool numeric(Type p)
        {
            if (p == Type.Char || p == Type.Int || p == Type.Double) return true;
            else return false;
        }

        public static Type max(Type p1, Type p2)
        {
            if (!numeric(p1) || !numeric(p2))
            {
                return null;
            }
            else if (p1 == Type.Double || p2 == Type.Double)
            {
                return Type.Double;
            }
            else if (p1 == Type.Int || p2 == Type.Int)
            {
                return Type.Int;
            }
            else
            {
                return Type.Char;
            }
        }
    }
}
