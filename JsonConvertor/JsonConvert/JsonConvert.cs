using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace			JsonConvert
{
	public class JsonConvert
	{
		/*
		 *	Recursively Flattens a Json Object to Propuce KeyValuePairs
		 *	so that tab : ["a", "!"] becomes {Key = "tab_1", Value = "a"}, {Key = "tab_2", Value = "!"}
		 *	and obj { prop : 1 } becomes {Key = "obj.prop", Value = 1}
		 */

		private class jValWrap
		{
			public string Type {get; set;}
			public object Value {get; set;}
			
			public override string ToString()
			{
				if (Value == null)
					return ("");
				return (Value.ToString());
			}
			
			public jValWrap(){}

			public jValWrap(JToken token)
			{
				Value = (token as JValue).Value;
				if (token.Type == JTokenType.String)
					Type = "varchar(255)";
				else if (token.Type == JTokenType.Integer)
					Type = "int";
				else
				{
					Type = "null";
				}
			}
		}

		private Dictionary<string, jValWrap> jFlattenerRecurse(string prefix, JToken token, Dictionary<string, jValWrap> jDict)
		{
			if (token.Type == JTokenType.Object)
			{
				prefix += (prefix.Length > 0) ? "." : "";
				foreach (JProperty jp in token.Children())
				{
					jFlattenerRecurse($"{prefix}{jp.Name}", jp.Value, jDict);
				}
			}
			else if (token.Type == JTokenType.Array)
			{
				int i = 0;
				foreach (JToken jt in token.Children())
				{
					jFlattenerRecurse($"{prefix}_{i}", jt, jDict);
					i++;
				}
			}
			else
			{
				jDict.Add(prefix, new jValWrap(token));
			}
			return (jDict);
		}

		/*
		 * Using the above Method, Return a List of JObjects converted to
		 * Dictionary<string, jValWrap>
		 */
		private List<Dictionary<string, jValWrap>> jflattener(string json)
		{
			JToken root = JToken.Parse(json);
			List<Dictionary<string, jValWrap>> set;
			set = new List<Dictionary<string, jValWrap>>();
			if (root.Type == JTokenType.Array)
			{
				foreach (JToken jt in root.Children())
				{
					set.Add(jFlattenerRecurse("", jt, new Dictionary<string, jValWrap>()));
				}
			}
			else
			{
				set.Add(jFlattenerRecurse("", root, new Dictionary<string, jValWrap>()));
			}
			normalise(set);
			return (set);
		}


		/*
		 *	With a list of JObject after conversion to Dictionary<string, jValWrap>,
		 *	this method checks thats each JObject contains the same keys in order 
		 *	to compare each jValWrap, later, in CSV or DataTable format.
		 */
		private void normaliseCheckKeyExists(string type, string key, List<Dictionary<string, jValWrap>> values)
		{
			foreach (var dict in values)
			{
				if (!dict.ContainsKey(key))
					dict.Add(key, new jValWrap(){Type = type, Value = null });
			}
		}

		private void normalise(List<Dictionary<string, jValWrap>> values)
		{
			foreach (var v in values)
			{
				foreach (var kv in v)
				{
					normaliseCheckKeyExists(kv.Value.Type, kv.Key, values);
				}
			}
			foreach (var kv in values)
			{
				normaliseCheckKeyExists("int", "ID", values);
			}
		}

		/*
		 * Concatenate function for csv values
		 */
		private string			csvCat(string dest, string seperator, object val)
		{
			string val_str = (val == null) ? "" : val.ToString();
			if (dest.Length > 0 && !dest.EndsWith(Environment.NewLine, StringComparison.InvariantCulture))
			{
				return (dest + seperator + val_str);
			}
			return (dest + val_str);
		}

		/*
		 *	Convert to CSV string using the string seperator 
		 */
		public string			JCSV(string json, string seperator)
		{
			var values = jflattener(json);
			var csv = values.First().Aggregate("", (curr, next) => { 
				return csvCat(curr, seperator, next.Key);
			});
			csv += "\n";
			foreach (var row in values)
			{
				csv = row.Aggregate(csv, (curr, next) => {
					return csvCat(curr, seperator, next.Value);
				});
				csv += "\n";
			}
			return (csv);
		}

		/*
		 * Convert to System.Data.DataTable
		 */

		private DataTable		jTable(List<Dictionary<string, jValWrap>> values)
		{
			DataTable dt = new DataTable();
			foreach (var kv in values.First())
			{
				dt.Columns.Add(kv.Key);
			}
			foreach (var node in values)
			{
				DataRow row = dt.NewRow();
				foreach (var kv in node)
				{
					Console.WriteLine(kv.Value);
					row[kv.Key] = kv.Value;
				}
				dt.Rows.Add(row);
			}
			return (dt);
		}

		/*
		 *	public JTable Converts Json to System.DataTable with optional
		 *	out value: sqlTypeDefinition, based on JToken.Types
		 */
		private string getSqlDefinition(Dictionary<string, jValWrap> dict, string table_name)
		{
			string def = $"CREATE TYPE dbo.{table_name} AS TABLE(\n";

			foreach (var kv in dict)
			{
				if (kv.Key == "ID")
				{
					def += $"	{kv.Key} {kv.Value.Type} NOT NULL\n";
				}
				else
					def += $"	{kv.Key} {kv.Value.Type} NULL\n";
			}
			def += "	PRIMARY KEY (ID)\n);";
			return (def);
		}


		public DataTable		JTable(string json)
		{
			return (jTable(jflattener(json)));
		}

		public DataTable		JTable(string json, string table_name, out string sqlDefinition)
		{
			var values = jflattener(json);
			sqlDefinition = getSqlDefinition(values.First(), table_name);
			return (jTable(values));
		}


	}
}
