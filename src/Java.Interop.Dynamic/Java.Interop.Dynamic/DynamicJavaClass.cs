﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Java.Interop;

using Mono.Linq.Expressions;

namespace Java.Interop.Dynamic {

	public class DynamicJavaClass : IDynamicMetaObjectProvider, IDisposable
	{
		public  string          JniClassName            {get; private set;}

		JavaClassInfo           info;
		bool                    disposed;

		public DynamicJavaClass (string jniClassName)
		{
			if (jniClassName == null)
				throw new ArgumentNullException ("jniClassName");

			JniClassName    = jniClassName;
			info            = JavaClassInfo.GetClassInfo (jniClassName);
		}

		public void Dispose ()
		{
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;

			if (disposed)
				return;

			info.Dispose ();
			info        = null;
			disposed    = true;
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject (Expression parameter)
		{
			return new MetaObject (parameter, this);
		}

#if false
		Type CreateManagedPeerType ()
		{
			var className   = JniClassName.Replace ('/', '-');
			var aname       = new AssemblyName ("Java.Interop.Dynamic-" + className);

			var assembly    = AppDomain.CurrentDomain.DefineDynamicAssembly (aname, AssemblyBuilderAccess.ReflectionOnly);
			var module      = assembly.DefineDynamicModule (className);
			var type        = module.DefineType (JniClassName, TypeAttributes.Sealed, typeof (JavaObject));
			type.SetCustomAttribute (
					new CustomAttributeBuilder (
						typeof (JniTypeInfoAttribute).GetConstructor (new[]{ typeof(string) }),
						new []{JniClassName}));

			return type.CreateType ();
		}
#endif

		class MetaObject : JniMetaObject
		{
			DynamicJavaClass    klass;

			public MetaObject (Expression parameter, DynamicJavaClass klass)
				: base (parameter, klass, klass.info)
			{
				this.klass  = klass;
			}

			protected override bool Disposed {
				get {return klass.disposed;}
			}

			protected override JniReferenceSafeHandle ConversionTarget {
				get {
					return klass.info.Members.JniPeerType.SafeHandle;
				}
			}

			protected override bool HasSelf {
				get {return false;}
			}

			protected override Expression GetSelf ()
			{
				return Expression.Constant (null, typeof (IJavaObject));
			}

			public override DynamicMetaObject BindInvoke (InvokeBinder binder, DynamicMetaObject[] args)
			{
				if (klass.info == null || klass.info.Disposed)
					return binder.FallbackInvoke (this, args);

				var constructors    = klass.info.Constructors;
				if (constructors == null)
					return binder.FallbackInvoke (this, args);

				if (Disposed) {
					return new DynamicMetaObject (ThrowObjectDisposedException (typeof (object)), BindingRestrictions.GetInstanceRestriction (Expression, Value));
				}

				var applicable  = constructors.Where (o => o.ArgumentTypes.Count == args.Length).ToArray ();
				if (applicable.Length == 0)
					return binder.FallbackInvoke (this, args);

				TryInvokeMember   invoke  = klass.info.TryInvokeMember;
				var value       = Expression.Parameter (typeof (object), "value");
				var fallback    = binder.FallbackInvoke (this, args);
				var call        = Expression.Block (
						new[]{value},
						Expression.Condition (
							test:       Expression.Call (Expression.Constant (klass.info), invoke.Method,
								Expression.Constant (null, typeof (IJavaObject)), Expression.Constant (applicable), Expression.Constant (args), value),
							ifTrue:     value,
							ifFalse:    fallback.Expression)
				);
				return new DynamicMetaObject (call, BindingRestrictions.GetInstanceRestriction (Expression, Value));
			}
		}
	}

	static class JavaModifiers {
		public  static  readonly    int     Static;

		static JavaModifiers ()
		{
			using (var t = new JniType ("java/lang/reflect/Modifier")) {
				using (var s = t.GetStaticField ("STATIC", "I"))
					Static  = s.GetInt32Value (t.SafeHandle);
			}
		}
	}

	struct JniArgumentMarshalInfo {
		JValue                          jvalue;
		JniLocalReference               lref;
		IJavaObject                     obj;
		Action<IJavaObject, object>     cleanup;

		internal JniArgumentMarshalInfo (object value, Type valueType)
		{
			this        = new JniArgumentMarshalInfo ();
			var jvm     = JniEnvironment.Current.JavaVM;
			var info    = jvm.GetJniMarshalInfoForType (valueType);
			if (info.CreateJValue != null)
				jvalue = info.CreateJValue (value);
			else if (info.CreateMarshalCollection != null) {
				obj     = info.CreateMarshalCollection (value);
				jvalue  = new JValue (obj);
			} else if (info.CreateLocalRef != null) {
				lref    = info.CreateLocalRef (value);
				jvalue  = new JValue (lref);
			} else
				throw new NotSupportedException ("Don't know how to get a JValue for: " + valueType.FullName);
			cleanup     = info.CleanupMarshalCollection;
		}

		public JValue JValue {
			get {return jvalue;}
		}

		public  void    Cleanup (object value)
		{
			if (cleanup != null && obj != null)
				cleanup (obj, value);
			if (lref != null)
				lref.Dispose ();
		}
	}
}

