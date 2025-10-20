using System;
using System.Collections.Generic;

namespace OpenTrader.Data
{
	public class HashTable<T> where T : class
	{
		class Entry { internal T Member; internal int ID; }
		List<Entry> Entries = new List<Entry>();

		public void Add(T member, int id)
		{
			Entry entry = new Entry() { Member = member, ID = id };
			Entries.Add(entry);
		}

		public int Find(T member)
		{
			Entry entry = Entries.Find(e => e.Member == member);
			if (entry == null)
				return 0;
			else
				return entry.ID;
		}

		public void Clear()
		{
			Entries.Clear();
		}
	}
}

