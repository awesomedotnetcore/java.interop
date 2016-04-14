using System;
using System.Collections.Generic;

namespace MonoDroid.Generation {

	public class XmlPullParserSymbol : ISymbol {

		public string DefaultValue {
			get { return "IntPtr.Zero"; }
		}

		public string FullName {
			get { return "System.Xml.XmlReader"; }
		}

		public bool IsGeneric {
			get { return false; }
		}

		public string JavaName {
			get { return "org.xmlpull.v1.XmlPullParser"; }
		}

		public string JniName {
			get { return "Lorg/xmlpull/v1/XmlPullParser;"; }
		}

		public string NativeType {
			get { return "IntPtr"; }
		}

		public bool IsEnum {
			get { return false; }
		}

		public bool IsArray {
			get { return false; }
		}

		public string ElementType {
			get { return null; }
		}

		public string GetGenericType (Dictionary<string, string> mappings)
		{
			return null;
		}

		public string FromNative (CodeGenerationOptions opt, string var_name, bool owned)
		{
			return String.Format (opt.GetOutputName ("Android.Runtime.XmlPullParserReader") + ".FromJniHandle ({0}, {1})", var_name, owned ? "JniHandleOwnership.TransferLocalRef" : "JniHandleOwnership.DoNotTransfer");
		}

		public string ToNative (CodeGenerationOptions opt, string var_name, Dictionary<string, string> mappings = null)
		{
			return String.Format ("global::Android.Runtime.XmlReaderPullParser.ToLocalJniHandle ({0})", var_name);
		}

		public bool Validate (CodeGenerationOptions opt, GenericParameterDefinitionList type_params)
		{
			return true;
		}

		public string Call (CodeGenerationOptions opt, string var_name)
		{
			return opt.GetSafeIdentifier (SymbolTable.GetNativeName (var_name));
		}

		public string[] PostCallback (CodeGenerationOptions opt, string var_name)
		{
			return new string [0];
		}

		public string[] PostCall (CodeGenerationOptions opt, string var_name)
		{
			return new string []{
				string.Format ("JNIEnv.DeleteLocalRef ({0});", opt.GetSafeIdentifier (SymbolTable.GetNativeName (var_name))),
			};
		}

		public string[] PreCallback (CodeGenerationOptions opt, string var_name, bool owned)
		{
			return new string[] { String.Format ("System.Xml.XmlReader {0} = global::Android.Runtime.XmlPullParserReader.FromJniHandle ({1}, JniHandleOwnership.DoNotTransfer);", opt.GetSafeIdentifier (var_name), opt.GetSafeIdentifier (SymbolTable.GetNativeName (var_name))) };
		}

		public string[] PreCall (CodeGenerationOptions opt, string var_name)
		{
			return new string[] { String.Format ("IntPtr {0} = global::Android.Runtime.XmlReaderPullParser.ToLocalJniHandle ({1});", opt.GetSafeIdentifier (SymbolTable.GetNativeName (var_name)), opt.GetSafeIdentifier (var_name)) };
		}

		public bool NeedsPrep { get { return true; } }
	}
}

