using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UELib.Core
{
	using UELib.Types;

	/// <summary>
	/// Represents a unreal property. 
	/// </summary>
	public partial class UProperty : UField
	{
		#region PreInitialized Members
		public PropertyType Type
		{
			get;
			protected set;
		}
		#endregion

		#region Serialized Members
		/// <summary>
		/// > 0 = True if UBoolProperty.
		/// </summary>
		public ushort 	ArrayDim
		{
			get;
			private set;
		}

		/// <summary>
		/// e.g. sizeof(int) etc
		/// </summary>
		public ushort 	ElementSize 
		{
			get;
			private set;
		}

		/// <value>
		/// 32bit in UE2
		/// 64bit in UE3
		/// </value>
		public ulong 	PropertyFlags 
		{
			get;
			private set;
		}

		public int 		CategoryIndex 
		{
			get;
			private set;
		}

		public ushort 	RepOffset 
		{
			get;
			private set;
		}
		#endregion

		#region General Members
		private bool _IsArray
		{
			get{ return ArrayDim > 1; }
		}

		public string CategoryName
		{
			get{ return CategoryIndex != -1 ? Package.NameTableList[CategoryIndex].Name : "@Null"; }
		}
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UProperty class. 
		/// </summary>
		public UProperty()
		{
			Type = PropertyType.None;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			// TODO: Read as Int32 then shift.
			ArrayDim = _Buffer.ReadUShort();
			NoteRead( "ArrayDim", ArrayDim );
			ElementSize = _Buffer.ReadUShort();
			NoteRead( "ElementSize", ElementSize );

			// TODO: Find out what version this was converted from DWORD to QWORD; This was a QWORD way before version 200!
			PropertyFlags = _Buffer.UR.ReadQWORDFlags();
			NoteRead( "PropertyFlags", PropertyFlags );
			if( !Package.IsConsoleCooked() )
			{
				CategoryIndex = _Buffer.ReadNameIndex();
				NoteRead( "CategoryIndex", CategoryIndex );
			}
			else CategoryIndex = -1;

			// TODO: UNKNOWN!
			if( Package.Version > 480 && !Package.IsConsoleCooked())
			{
				int unk = _Buffer.ReadInt32();
				NoteRead( "Unknown", unk );
			}

			if( HasPropertyFlag( Flags.PropertyFlagsLO.Net ) )
			{
				RepOffset = _Buffer.ReadUShort();
				NoteRead( "RepOffset", RepOffset );
			}

			if( HasPropertyFlag( Flags.PropertyFlagsLO.New ) && Package.Version <= (uint)UnrealPackage.GameVersions.UT2K4 )
			{
				string unknown = _Buffer.ReadName();
				Console.WriteLine( "Found a property flagged with New:" + unknown );
			}

#if SWAT4
			if( Package.LicenseeVersion == (ushort)UnrealPackage.LicenseeVersions.Swat4 )
			{
				// Contains meta data such as a ToolTip.
				_Buffer.Skip( 3 );
			}
#endif
		}
	
		#region Methods
		public bool HasPropertyFlag( Flags.PropertyFlagsLO flag )
		{
			return ((uint)(PropertyFlags & 0x00000000FFFFFFFFU) & (uint)flag) != 0; 
		}

		public bool HasPropertyFlag( Flags.PropertyFlagsHO flag )
		{
			return ((PropertyFlags >> 32) & (uint)flag) != 0; 
		}

		public bool IsParm()
		{
			return HasPropertyFlag( Flags.PropertyFlagsLO.Parm );
		}

		public virtual string GetFriendlyInnerType()
		{
			return String.Empty;
		}
		#endregion
	}

	/// <summary>
	/// Interface Property
	/// 
	/// UE3 Only
	/// </summary>
	public class UInterfaceProperty : UProperty
	{
		#region Serialized Members
		public UClass InterfaceObject = null;
		public UInterfaceProperty InterfaceType = null;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UInterfaceProperty class. 
		/// </summary>
		public UInterfaceProperty()
		{
			Type = PropertyType.InterfaceProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int Index = _Buffer.ReadObjectIndex();
			InterfaceObject = (UClass)GetIndexObject( Index );

			//Index = _Buffer.ReadObjectIndex();
			//_InterfaceType = (UInterfaceProperty)GetIndexObject( Index );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "interface";
		}
	}

	/// <summary>
	/// Delegate Property
	/// 
	/// UE2+
	/// </summary>
	public class UDelegateProperty : UProperty
	{
		#region Serialized Members
		public UObject FunctionObject = null;
		public UObject DelegateObject = null;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UDelegateProperty class. 
		/// </summary>
		public UDelegateProperty()
		{
			Type = PropertyType.DelegateProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			FunctionObject = GetIndexObject( _Buffer.ReadObjectIndex() );
			if( Package.Version > 184 )
			{
				DelegateObject = GetIndexObject( _Buffer.ReadObjectIndex() );
			}
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "delegate<" + GetFriendlyInnerType() + ">";
		}

		public override string GetFriendlyInnerType()
		{
			return FunctionObject != null ? FunctionObject.GetFriendlyType() : "@NULL";
		}
	}

	/// <summary>
	/// Pointer Property
	/// 
	/// UE2 Only (UStructProperty in UE3)
	/// </summary>
	public class UPointerProperty : UProperty
	{
		/// <summary>
		/// Creates a new instance of the UELib.Core.UPointerProperty class. 
		/// </summary>
		public UPointerProperty()
		{
			Type = PropertyType.PointerProperty;
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "pointer";
		}
	}

	/// <summary>
	/// Component Property
	/// 
	/// UE3 Only
	/// </summary>
	public class UComponentProperty : UObjectProperty
	{
		/// <summary>
		/// Creates a new instance of the UELib.Core.UComponentProperty class. 
		/// </summary>
		public UComponentProperty()
		{
			Type = PropertyType.ComponentProperty;
		}
	}

	/// <summary>
	/// Class Property
	/// 
	/// var class'Actor' ActorClass;
	/// </summary>
	public class UClassProperty : UObjectProperty
	{
		#region Serialized Members
		// MetaClass
		public UClass ClassObject = null;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UClassProperty class. 
		/// </summary>
		public UClassProperty()
		{
			Type = PropertyType.ClassProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int ClassIndex = _Buffer.ReadObjectIndex();
			ClassObject = (UClass)GetIndexObject( ClassIndex );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			if( ClassObject != null )
			{
				return (String.Compare( ClassObject.Name, "Object", true ) == 0) 
					? Object.GetFriendlyType() 
					: ("class" + "<" + GetFriendlyInnerType() + ">");
			}
			return "class";
		}

		public override string GetFriendlyInnerType()
		{
			return ClassObject != null ? ClassObject.GetFriendlyType() : "@NULL";
		}
	}

	/// <summary>
	/// Fixed Array Property
	/// </summary>
	public class UFixedArrayProperty : UProperty
	{
		#region Serialized Members
		public UProperty InnerObject = null;

		private int _Count = 0;
		public int Count
		{
			get{ return _Count; }
		}
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UFixedArrayProperty class. 
		/// </summary>
		public UFixedArrayProperty()
		{
			Type = PropertyType.FixedArrayProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int InnerIndex = _Buffer.ReadObjectIndex();
			InnerObject = (UProperty)GetIndexObject( InnerIndex );
			_Count = _Buffer.ReadIndex();
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			// Just move to decompiling?
			return base.GetFriendlyType() + "[" + _Count + "]";
		}
	}

	/// <summary>
	/// Dynamic Array Property
	/// </summary>
	public class UArrayProperty : UProperty
	{
		#region Serialized Members
		public UProperty InnerProperty = null;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UArrayProperty class. 
		/// </summary>
		public UArrayProperty()
		{
			Type = PropertyType.ArrayProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int InnerIndex = _Buffer.ReadObjectIndex();
			InnerProperty = (UProperty)GetIndexObject( InnerIndex );
		}
	
		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			if( InnerProperty != null )
			{
				return "array" + "<" + GetFriendlyInnerType() + ">";
			}
			return "array";
		}

		public override string GetFriendlyInnerType()
		{
			return InnerProperty != null 
				? (InnerProperty.IsClassType( "ClassProperty" ) || InnerProperty.IsClassType( "DelegateProperty" )) 
					? (" " + InnerProperty.FormatFlags() + InnerProperty.GetFriendlyType() + " ") 
					: (InnerProperty.FormatFlags() + InnerProperty.GetFriendlyType())
				: "@NULL";
		}
	}

	/// <summary>
	/// Dynamic Map Property
	/// 
	/// Obsolete
	/// </summary>
	public class UMapProperty : UProperty
	{
		#region Serialized Members
		private int _Key;
		private int _Value;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UMapProperty class. 
		/// </summary>
		public UMapProperty()
		{
			Type = PropertyType.MapProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			_Key = _Buffer.ReadObjectIndex();
			_Value = _Buffer.ReadObjectIndex();
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "map<" + _Key + ", " + _Value + ">";
		}
	}

	/// <summary>
	/// Struct Property
	/// </summary>
	public class UStructProperty : UProperty
	{
		#region Serialized Members
		public UStruct StructObject = null;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UStructProperty class. 
		/// </summary>
		public UStructProperty()
		{
			Type = PropertyType.StructProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			StructObject = (UStruct)GetIndexObject( _Buffer.ReadObjectIndex() );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return StructObject != null ? StructObject.GetOuterGroup() : "@NULL";
		}
	}

	/// <summary>
	/// Byte Property
	/// </summary>
	public class UByteProperty : UProperty
	{
		#region Serialized Members
		public UEnum EnumObject = null;
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UByteProperty class. 
		/// </summary>
		public UByteProperty()
		{
			Type = PropertyType.ByteProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int EnumIndex = _Buffer.ReadObjectIndex();
			EnumObject = (UEnum)GetIndexObject( EnumIndex );
		}

		/// <inheritdoc/>
		public override void InitializeImports()
		{
			base.InitializeImports();
			ImportObject();
		}

		// Import the enum of e.g. Actor.Role and LevelInfo.NetMode.
		private void ImportObject()
		{
			// Already imported...
			if( EnumObject != null )
			{
				return;
			}

			UnrealPackage pkg = LoadImportPackage();
			if( pkg != null )
			{		
				if( pkg.ObjectsList == null )
				{
					pkg.RegisterClass( "ByteProperty", typeof(UByteProperty) );
					pkg.RegisterClass( "Enum", typeof(UEnum) );
					pkg.InitializeExportObjects();
				}
				UByteProperty B = (UByteProperty)pkg.FindObject( Name, typeof(UByteProperty) );
				if( B != null )
				{
					EnumObject = B.EnumObject;
				}
			}
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			if( EnumObject != null )
			{
				return EnumObject.GetOuterGroup();
			}
			return "byte";
		}
	}

	/// <summary>
	/// Int Property
	/// </summary>
	public class UIntProperty : UProperty
	{
		/// <summary>
		/// Creates a new instance of the UELib.Core.UIntProperty class. 
		/// </summary>
		public UIntProperty()
		{
			Type = PropertyType.IntProperty;
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "int";
		}
	}

	/// <summary>
	/// Bool Property
	/// </summary>
	public class UBoolProperty : UProperty
	{
		/// <summary>
		///	Creates a new instance of the UELib.Core.UBoolProperty class. 
		/// </summary>
		public UBoolProperty()
		{
			Type = PropertyType.BoolProperty;
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "bool";
		}

		// TODO: Figure out 2 unknown conditions where bool has a UInt32 variable.
		/*protected override void Deserialize()
		{
			base.Deserialize();

			if( ?? )
			{
				// BitMask?
				_Buffer.ReadUInt32();
			}
		}*/
	}

	/// <summary>
	/// Float Property
	/// </summary>
	public class UFloatProperty : UProperty
	{
		/// <summary>
		///	Creates a new instance of the UELib.Core.UFloatProperty class. 
		/// </summary>
		public UFloatProperty()
		{
			Type = PropertyType.FloatProperty;
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "float";
		}
	}

	/// <summary>
	/// Name Property
	/// </summary>
	public class UNameProperty : UProperty
	{
		/// <summary>
		///	Creates a new instance of the UELib.Core.UNameProperty class. 
		/// </summary>
		public UNameProperty()
		{
			Type = PropertyType.NameProperty;
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "name";
		}
	}

	/// <summary>
	/// Dynamic String
	/// </summary>
	public class UStrProperty : UProperty
	{
		/// <summary>
		/// Creates a new instance of the UELib.Core.UStrProperty class. 
		/// </summary>
		public UStrProperty()
		{
			Type = PropertyType.StrProperty;
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "string";
		}
	}

	/// <summary>
	/// Fixed String
	/// 
	/// UE1 Only
	/// </summary>
	public class UStringProperty : UProperty
	{
		public int Size;

		/// <summary>
		/// Creates a new instance of the UELib.Core.UStringProperty class. 
		/// </summary>
		public UStringProperty()
		{
			Type = PropertyType.StringProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			Size = _Buffer.ReadInt32();
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return "string[" + Size + "]";
		}
	}

	/// <summary>
	/// Object Reference Property
	/// </summary>
	public class UObjectProperty : UProperty
	{
		#region Serialized Members
		public UObject Object
		{
			get;
			private set;
		}
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UObjectProperty class. 
		/// </summary>
		public UObjectProperty()
		{
			Type = PropertyType.ObjectProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int ObjectIndex = _Buffer.ReadObjectIndex();
			Object = GetIndexObject( ObjectIndex );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return Object != null ? Object.GetFriendlyType() : "@NULL";
		}
	}
}