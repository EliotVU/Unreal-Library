using UELib.Core;

namespace UELib
{
	/// <summary>
	/// Allows an object to decompile itself into text.
	/// </summary>
	public interface IUnrealDecompileable
	{
		/// <summary>
		/// Decompile this object.
		/// </summary>
		/// <returns>The decompiled ouput of text.</returns>
		string Decompile();
	}

	/// <summary>
	/// Allows an object to be tested whether it can may be decompiled.
	/// </summary>
	public interface IDecompileableNode : IUnrealDecompileable
	{
		// FIXME: Internal hack for Unreal Explorer!
		string Text{ get; set; }

		/// <summary>
		/// Whether the node allows decompiling a.t.m.
		/// </summary>
		bool AllowDecompile{ get; }
	}
			  
	/// <summary>
	/// Allows a node to be attached with a decompileable object.
	/// </summary>
	public interface IDecompileableObjectNode : IDecompileableNode
	{
		/// <summary>
		/// The decompileable object that will be decompiled when this object's Decompile() function is called.
		/// </summary>
		IUnrealDecompileable Object{ get; set; }

		/// <summary>
		/// Whether the object has a byte's buffer.
		/// </summary>
		bool CanViewBuffer{ get; }
	}

	/// <summary>
	/// Supports a buffer.
	/// </summary>
	public interface ISupportsBuffer
	{
		/// <summary>
		/// Get a copy of a buffer.
		/// </summary>
		/// <returns>The copyed buffer.</returns>
		 byte[] GetBuffer();
	}

	/// <summary>
	/// Allos an object to show itself to the user.
	/// </summary>
	public interface IUnrealViewable
	{
		/// <summary>
		/// View this instanced object.
		/// </summary>
		void View();
	}

	public interface IUnrealDeserializableClass
	{
		void Deserialize( IUnrealStream stream );
	}

	public interface IUnrealDeserializableObject
	{
		/// <summary>
		/// Copy of bytes from the current instanced Object.
		/// </summary>
		UObjectStream Buffer{ get; }

		void BeginDeserializing();
	}

	// Used by EventArg's
	public interface IRefUObject
	{
		UObject ObjectRef{ get; }
	}

	/// <summary>
	/// This class is exportable into an non-unreal format
	/// </summary>
	public interface IUnrealExportable
	{
		string[] ExportableExtensions{ get; }

		bool CompatableExport();
		void SerializeExport( string desiredExportExtension, System.IO.FileStream exportStream );
	}
}
