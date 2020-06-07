using System.Collections;

namespace symbols
{
	using lexer;
	using inter;

	public class Env
	{
		private Hashtable table;
		protected internal Env prev;

		public Env(Env n)
		{
			table = new Hashtable();
			prev = n;
		}

		public virtual void put(Token w, Id i) 
		{
			table[w] = i;
		}

		public virtual Id get(Token w)
		{
			for (Env e = this; e != null; e = e.prev)
			{
				Id found = (Id)(e.table[w]);
				if (found != null)
				{
					return found;
				}
			}
			return null;
		}
	}
}
