using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace TestCompiler
{
	public class Env
	{

		private Hashtable table;
		protected Env prev;

		public Env(Env n) { table = new Hashtable(); prev = n; }

		public void put(Token w, Id i) { table.Add(w, i); }

		public Id get(Token w)
		{
			for (Env e = this; e != null; e = e.prev)
			{
				Id found = (Id)(e.table[w]);
				if (found != null) return found;
			}
			return null;
		}
	}
}
