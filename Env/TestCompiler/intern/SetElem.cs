namespace inter
{
    using lexer;
    using symbols;

    public class SetElem : Stmt
    {
        public Id array; public Expr index; public Expr expr;

        public SetElem(Access x, Expr y)
        {
            array = x.array; 
            index = x.index; 
            expr = y;
            if (check(x.type, expr.type) == null)
            {
                error("type error");
            }
        }

        public virtual Type check(Type p1, Type p2)
        {
            if (p1 is Array || p2 is Array)
            {
                return null;
            }
            else if (p1 == p2)
            {
                return p2;
            }
            else if (Type.numeric(p1) && Type.numeric(p2))
            {
                return p2;
            }
            else
            {
                return null;
            }
        }

        public override void gen(int b, int a)
        {
            string s1 = index.reduce().ToString();
            string s2 = expr.reduce().ToString();
            emit(array.ToString() + " [ " + s1 + " ] = " + s2);
        }
    }
}
