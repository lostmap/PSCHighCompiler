namespace inter
{
    public class Stmt : Node
    {
        public Stmt() { }

        public static Stmt Null = new Stmt();

        public virtual void gen(int b, int a) { } // called with labels begin and after

        internal int after = 0;                   // saves label after
        public static Stmt Enclosing = Stmt.Null;  // used for break stmts
    }
}
