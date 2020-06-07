namespace inter
{
	using lexer;
	using symbols;

	public class Id : Expr
	{
		public int offset;     // relative address

		public Id(Word id, Type p, int b) : base(id, p) 
		{
			offset = b;
		}
		//	public String toString() {return "" + op.toString() + offset;}
	}
}
