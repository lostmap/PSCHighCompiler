﻿namespace inter
{
    using lexer;
    using symbols;

    public class Access : Op
    {
        public Id array;
        public Expr index;

        public Access(Id a, Expr i, Type p) : base(new Word("[]", Tag.INDEX), p) // flattening the array
        {    // p is element type after
            array = a;
            index = i;
        }

        public override Expr gen()
        { 
            return new Access(array, index.reduce(), type); 
        }

        public override void jumping(int t, int f)
        { 
            emitjumps(reduce().ToString(), t, f); 
        }

        public override string ToString()
        {
            return array.ToString() + " [ " + index.ToString() + " ]";
        }
    }
}
