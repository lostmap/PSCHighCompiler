using Microsoft.VisualBasic;
using symbols;

namespace lexer
{
    public class VariantToken : Token
    {
        public readonly Variant value;
        public readonly int varType;

        public VariantToken(double v) : base(Tag.VARIANT)
        {
            value = new Variant(v);
            varType = (int)Variant.VarType.NUM;
        }
        public VariantToken(int v) : base(Tag.VARIANT)
        {
            value = new Variant(v);
            varType = (int) Variant.VarType.NUM;
        }

        public override string ToString()
        {
            return "" + value.ToString();
        }
    }
}