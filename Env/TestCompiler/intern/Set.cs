namespace inter
{
    using lexer;
    using symbols;
    using System.Diagnostics;

    public class Set : Stmt
    {
        public Id id; 
        public Expr expr;

        public Set(Id i, Expr x)
        {
            id = i; expr = x;
            if (check(id.type, expr.type) == null)
            {
                error("type error");
            }
        }

        public virtual Type check(Type p1, Type p2)
        {
            if (Type.numeric(p1) && Type.numeric(p2))
            {
                return p2;
            }
            else if (p1 == Type.Bool && p2 == Type.Bool)
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
            emit(id.ToString() + " = " + expr.gen().ToString());
        }
        public override void bytecode(Env currEnv)
        { 
             
        }
    }
}
