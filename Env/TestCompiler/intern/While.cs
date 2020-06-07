namespace inter
{
    using symbols;

    public class While : Stmt
    {
        internal Expr expr;
        internal Stmt stmt;

        public While()
        { 
            expr = null; 
            stmt = null;
        }

        public virtual void init(Expr x, Stmt s)
        {
            expr = x;
            stmt = s;
            if (expr.type != Type.Bool)
            {
                expr.error("boolean required in while");
            }
        }
        public override void gen(int b, int a)
        {
            after = a;                // save label a
            expr.jumping(0, a);
            int label = newlabel();   // label for stmt
            emitlabel(label); stmt.gen(label, b);
            emit("goto L" + b);
        }
    }
}
