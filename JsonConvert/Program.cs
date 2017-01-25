using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Data;

namespace JsonConvert
{
	class			Program
	{
		public static class Out
		{
			public static void Put(object o) { Console.WriteLine(o.ToString());}
			public static void Put(string name, object o) { Put(name); Put(o);}
			public static void Print(string name, object o) { Console.Write(name); Console.Write(o);}
			public static void Print(object o) { Console.Write(o);}
		}

		public static void TestCase(JToken jt)
		{
			Out.Put("--== JToken parsed as: ");
			Out.Put(jt.ToString());
			Out.Put("--= =As Csv: ");
			Out.Put(new JsonConvert().JCSV(jt.ToString(), ","));
		}
		public static void Assert_Equal<T>(T a, T b)
		{
			if (!a.Equals(b))
				throw new Exception($"{a.ToString()} != {b.ToString()} : Assertion Failed");

		}

		public static void Main(string[] args)
		{
			TestCase(JToken.FromObject(new int[] {1, 2, 3}));
			TestCase(JToken.FromObject(new Dictionary<string, int []>(){
				{"first", new int[]{1, 2, 3}},
				{"second", new int[]{4, 5, 6}}
			}));
			var obj = JToken.FromObject(new {
				str = "simple",
				num = 1,
				tab = new string[] {"how", "to", "manage", "this"},
				obj = new {
					str = "nested",
					num = 2,
					tab = new string[]{
						"create", "new", "sql_tab",
						"with_foreign_key", "?" }
				}
			});
			TestCase(obj);
			var toDt = new JsonConvert();
			string sqlDefinition;
			DataTable dt = toDt.JTable(obj.ToString(), "Example", out sqlDefinition);
			Assert_Equal(dt.Columns[0].ColumnName.ToString(), "str");
			Assert_Equal(dt.Rows[0][0].ToString(), "simple");
			Out.Put(sqlDefinition);
		}
	}
}
