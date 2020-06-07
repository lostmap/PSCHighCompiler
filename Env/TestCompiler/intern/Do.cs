namespace inter
{
    using symbols;

    public class Do : Stmt
    {
        internal Expr expr;
        internal Stmt stmt;

        public Do() 
        {
            expr = null; 
            stmt = null;
        }

        public virtual void init(Stmt s, Expr x)
        {
            expr = x; stmt = s;
            if (expr.type != Type.Bool)
            {
                expr.error("boolean required in do");
            }
        }

        public override void gen(int b, int a)
        {
            after = a;
            int label = newlabel();   // label for expr
            stmt.gen(b, label);
            emitlabel(label);
            expr.jumping(b, 0);
        }
    }
}
