using System;
using System.Runtime.InteropServices;

namespace Java.Interop
{
	partial class JniEnvironment {

		static partial class References
		{
			public static void Dispose (ref JniObjectReference reference)
			{
				Dispose (ref reference, JniObjectReferenceOptions.DisposeSourceReference);
			}

			const JniObjectReferenceOptions TransferMask    = JniObjectReferenceOptions.CreateNewReference | JniObjectReferenceOptions.DisposeSourceReference;

			public static void Dispose (ref JniObjectReference reference, JniObjectReferenceOptions transfer)
			{
				if (!reference.IsValid)
					return;

				switch (transfer & TransferMask) {
				case JniObjectReferenceOptions.CreateNewReference:
					break;
				case JniObjectReferenceOptions.DisposeSourceReference:
					switch (reference.Type) {
					case JniObjectReferenceType.Global:
						JniEnvironment.Runtime.ObjectReferenceManager.DeleteGlobalReference (ref reference);
						break;
					case JniObjectReferenceType.Local:
						JniEnvironment.Runtime.ObjectReferenceManager.DeleteLocalReference (JniEnvironment.CurrentInfo, ref reference);
						break;
					case JniObjectReferenceType.WeakGlobal:
						JniEnvironment.Runtime.ObjectReferenceManager.DeleteWeakGlobalReference (ref reference);
						break;
					default:
						throw new NotImplementedException ("Do not know how to dispose: " + reference.Type + ".");
					}
					reference.Invalidate ();
					break;
				default:
					throw new NotImplementedException ("Do not know how to transfer: " + transfer);
				}
			}

#if !XA_INTEGRATION
			public static int GetIdentityHashCode (JniObjectReference value)
			{
				return JniSystem.IdentityHashCode (value);
			}
#endif  // !XA_INTEGRATION

			public static IntPtr NewReturnToJniRef (IJavaPeerable value)
			{
				if (value == null || !value.PeerReference.IsValid)
					return IntPtr.Zero;
				return NewReturnToJniRef (value.PeerReference);
			}

			public static IntPtr NewReturnToJniRef (JniObjectReference value)
			{
				if (!value.IsValid)
					return IntPtr.Zero;
				var l = value.NewLocalRef ();
				return JniEnvironment.Runtime.ObjectReferenceManager.ReleaseLocalReference (JniEnvironment.CurrentInfo, ref l);
			}
		}
	}
}

