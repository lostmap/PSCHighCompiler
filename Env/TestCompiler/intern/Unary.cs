namespace inter
{
    using lexer;
    using symbols;

    public class Unary : Op
    {
        public Expr expr;

        public Unary(Token tok, Expr x) : base(tok, null)
        {    // handles minus, for ! see Not
            expr = x;
            type = Type.max(Type.Int, expr.type);
            if (type == null)
            {
                error("type error");
            }
        }

        public override Expr gen()
        { 
            return new Unary(op, expr.reduce());
        }

        public override string ToString()
        {
            return op.ToString() + " " + expr.ToString(); 
        }
    }
}
