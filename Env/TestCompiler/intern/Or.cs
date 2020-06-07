﻿namespace inter
{
    using lexer;
    using symbols;

    public class Or : Logical
    {
        public Or(Token tok, Expr x1, Expr x2) : base(tok, x1, x2) { }

        public override void jumping(int t, int f)
        {
            int label = t != 0 ? t : newlabel();
            expr1.jumping(label, 0);
            expr2.jumping(t, f);
            if (t == 0)
            {
                emitlabel(label);
            }
        }
    }
}
