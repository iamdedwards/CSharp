using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace XamlSamples
{
	public class KeypadPage
	{
		public KeypadPage()
		{
		
		}
		public ICommand DeleteCharCommand;
		public ICommand AddCharCommand;
		public ICommand DisplayText;
	}
}

namespace XamlToJs
{
	static class Help
	{
		public static void MethodsOrProps(Type t, string search)
		{
			var methods = MethodsAndProps(t);
			var lookup = methods.Find((m) => {
				return (m.ToUpper().Contains(search.ToUpper()));
			});
			Console.WriteLine(lookup);
		}

		public static List<string> MethodsAndProps(Type t)
		{
			var methods = t.GetMethods().ToList().Select(m => m.ToString()).ToList();
			methods.AddRange(t.GetProperties().ToList().Select(m => m.ToString()));
			foreach (var mp in methods)
			{
				Console.WriteLine(mp);
			}
			return (methods);
		}
	}

	public class XCommand
	{
		public string Name;
		public object Param;
	}
	public class XClass
	{
		public string NameSpace {get; set;}
		public string Assembly {get; set;}
		public string Name {get; set;}
		private List<XCommand> _xCommands = null;
		public virtual List<XCommand> XCommands {
			get {
					_xCommands = _xCommands ?? new List<XCommand>();
					return (_xCommands);
				}
			private set { }
		}

		public XClass()
		{
			XCommands = new List<XCommand>();
		}
	}

	class MainClass
	{
		public static Action<string, object> P = (n, o) => {
			Console.Write(n);
			Console.Write(" = ");
			Console.WriteLine(o.ToString());
		}; 
		
		public static void XRecurse(XElement elem, Action<XElement> act)
		{
			act(elem);
			var nodes = elem.Nodes();
			foreach (var n in nodes)
			{
				if (n is XElement)
					XRecurse(n as XElement, act);
			}
		}

		private static bool setXLocal(IEnumerable<XAttribute> attrs, XClass xclass)
		{
			var local_attr = attrs.SingleOrDefault((l) => {
				return (l.Name.ToString().Contains("local"));
			});
			if (local_attr == null)
				return (false);

			var local = local_attr.ToString();
			var pattern = 
				"(xmlns:local=\")" +
				"(clr-namespace:)" +
				"([^;]*);"+
				"(assembly=)"+
				"([^\"]*)\"";
			var scan = new Regex(pattern);
			var match = scan.Match(local.ToString());
			if (match.Success)
			{
				xclass.NameSpace = match.Groups[3].Value;
				xclass.Assembly = match.Groups[5].Value;
				return (true);
			}
			return (false);
		}

		public static bool setXClassName(IEnumerable<XAttribute> attrs,
									XClass xclass)
		{
			var class_attr = attrs.SingleOrDefault((a) => {
				return a.Name.ToString().Contains("Class");
			});
			if (class_attr == null)
				return (false);
			string class_str = class_attr.ToString();
			int class_name_index = class_str.IndexOf("Class=");
			class_name_index += "Class=".Length;
			int len = class_str.Length - class_name_index;
			class_str = class_str.Substring(class_name_index, len);
			class_str = class_str.Replace("\"", "");
			xclass.Name = class_str;
			return (true);
		}

		public static bool setXClass(IEnumerable<XAttribute> attrs,
									XClass xclass)
		{
			if (xclass.Name != null &&
				xclass.NameSpace != null &&
				xclass.Assembly != null)
				return (true);
			setXLocal(attrs, xclass);
			setXClassName(attrs, xclass);
			return (false);
		}

		public static void getXClassCommands(IEnumerable<XAttribute> attrs,
									XClass xclass)
		{
			bool bound = attrs.Any((a) => {return a.ToString().Contains("Binding");});
			if (bound)
			{
				string flat = attrs.Aggregate("", (a, b) => {
					return (a.ToString() + b.ToString());
				});
				var scan = new Regex(
					"Command=\"{Binding (.*)}\"" +
					"CommandParameter=\"([^\"]*)\"" +
					".*"
					);
				var match = scan.Match(flat);
				if (match.Success)
				{
					xclass.XCommands.Add(new XCommand()
						{ Name = match.Groups[1].ToString(), Param = match.Groups[2].ToString()});
				}
			}
		}

		public static void Main (string[] args)
		{
			var doc = XDocument.Load("/home/saradan/xaml/keypad.xaml");
			var xClass = new XClass();
			bool found = false;
			XRecurse(doc.Root, (elem) => {
				var attrs = elem.Attributes();
				if (!found)
					found = setXClass(attrs, xClass);
				getXClassCommands(attrs, xClass);
			});
		}
	}
}

