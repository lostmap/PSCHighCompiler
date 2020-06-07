namespace inter
{
    using lexer;
    using symbols;

    public class Not : Logical
    {
        public Not(Token tok, Expr x2) : base(tok, x2, x2) { }

        public override void jumping(int t, int f) 
        {
            expr2.jumping(f, t); 
        }

        public override string ToString() 
        { 
            return op.ToString() + " " + expr2.ToString(); 
        }
    }
}
