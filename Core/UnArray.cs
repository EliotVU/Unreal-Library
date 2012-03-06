using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib.Core
{
	//Mirrored docu' from List<T>

	/// <summary>
	/// Represents a strongly typed array of serializable classes that can be accessed by index. 
	/// Provides methods to serialize from a specified stream. 
	/// </summary>
	/// <typeparam name="T">T must have interface UELib.IUnrealSerializableClass and have default empty constructor.</typeparam>
	public class UArray<T> : List<T> where T : IUnrealDeserializableClass, new()
	{
		/// <summary>
		/// Initializes a new instance of the UELib.Core.UArray'T' class
		/// that is empty and has the default initial capacity.
		/// </summary>
		public UArray() : base()
		{
		}

		/// <summary>
		/// Initialize a new instance based from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to use for initializing this array.</param>
		public UArray( IUnrealStream stream ) : base()
		{
			Deserialize( stream );
		}

		/// <summary>
		/// Initialize a new instance based from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to use for initializing this array.</param>
		public UArray( IUnrealStream stream, int count ) : base()
		{
			Deserialize( stream, count );
		}

		/// <summary>
		/// <see cref="System.Collections.Generic.List<T>.List( collection )"/>
		/// </summary>
		public UArray( UArray<T> collection ) : base( collection )
		{
		}

		/// <summary>
		/// Initialize this array with items in the specified stream.
		/// </summary>
		/// <param name="stream">The stream to use for initializing this array.</param>
		public void Deserialize( IUnrealStream stream )
		{
			int c = stream.ReadIndex();
			Capacity = c;
			for( int i = 0; i < c; ++ i )
			{
				T item = new T();
				item.Deserialize( stream );
				Add( item );
			}
		}

		/// <summary>
		/// Initialize this array with items in the specified stream.
		/// </summary>
		/// <param name="stream">The stream to use for initializing this array.</param>
		public void Deserialize( IUnrealStream stream, int count )
		{
			Capacity = count;
			for( int i = 0; i < count; ++ i )
			{
				T item = new T();
				item.Deserialize( stream );
				Add( item );
			}
		}

		/// <summary>
		/// Initialize this array with items in the specified stream.
		/// </summary>
		/// <param name="stream">The stream to use for initializing this array.</param>
		/// <param name="action">The action to invoke before serializing a item.</param>
		public void Deserialize( IUnrealStream stream, Action<T> action )
		{
			int c = stream.ReadIndex();
			Capacity = c;
			for( int i = 0; i < c; ++ i )
			{
				T item = new T();
				action.Invoke( item );
				item.Deserialize( stream );	
				Add( item );
			}
		}
	}

	public static class UArrayList 
	{
		public static void Deserialize( this List<int> indexes, IUnrealStream stream )
		{
			indexes.Capacity = stream.ReadInt32();
			for( int i = 0; i < indexes.Capacity; ++ i )
			{
				indexes.Add( stream.ReadIndex() );
			}
		}
	}
}
