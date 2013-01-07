using System;

namespace UELib.Core
{
	using Types;

	/// <summary>
	/// Represents a unreal property. 
	/// </summary>
	public partial class UProperty : UField, IUnrealNetObject
	{
		#region PreInitialized Members
		public PropertyType Type
		{
			get;
			protected set;
		}
		#endregion

		#region Serialized Members
		public ushort 	ArrayDim
		{
			get;
			private set;
		}

		public ushort 	ElementSize 
		{
			get;
			private set;
		}

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

		public UEnum	ArrayEnum{ get; private set; }

		public ushort 	RepOffset 
		{
			get;
			private set;
		}

		public bool		RepReliable
		{
			get{ return HasPropertyFlag( Flags.PropertyFlagsLO.Net ); }
		}

		public uint		RepKey
		{
			get{ return RepOffset | ((uint)Convert.ToByte( RepReliable ) << 16); }
		}
		#endregion

		#region General Members
		private bool _IsArray
		{
			get{ return ArrayDim > 1; }
		}

		public string CategoryName
		{
			get{ return CategoryIndex != -1 ? Package.Names[CategoryIndex].Name : "@Null"; }
		}
		#endregion

		#region Constructors
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

#if XIII
			if( Package.Build == UnrealPackage.GameBuild.BuildName.XIII )
			{
				ArrayDim = _Buffer.ReadUShort();
				Record( "ArrayDim", ArrayDim );		
				goto skipInfo;
			}
#endif

			var info = _Buffer.ReadUInt32();
			ArrayDim = (ushort)(info & 0x0000FFFFU);
			Record( "ArrayDim", ArrayDim );
			ElementSize = (ushort)(info >> 16);
			Record( "ElementSize", ElementSize );
			skipInfo:

			PropertyFlags = Package.Version >= 220 ? _Buffer.ReadUInt64() : _Buffer.ReadUInt32();
			Record( "PropertyFlags", PropertyFlags );
			if( !Package.IsConsoleCooked() )
			{
				CategoryIndex = _Buffer.ReadNameIndex();
				Record( "CategoryIndex", CategoryIndex );

				if( Package.Version > 400 )
				{
					ArrayEnum = GetIndexObject( _Buffer.ReadObjectIndex() ) as UEnum;
					Record( "ArrayEnum", ArrayEnum );
				}
			}
			else CategoryIndex = -1;

			if( HasPropertyFlag( Flags.PropertyFlagsLO.Net ) )
			{
				RepOffset = _Buffer.ReadUShort();
				Record( "RepOffset", RepOffset );
			}

			if( HasPropertyFlag( Flags.PropertyFlagsLO.New ) && Package.Version <= 128 )
			{
				string unknown = _Buffer.ReadText();
				Console.WriteLine( "Found a property flagged with New:" + unknown );
			}

#if SWAT4
			if( Package.Build == UnrealPackage.GameBuild.BuildName.Swat4 )
			{
				// Contains meta data such as a ToolTip.
				_Buffer.Skip( 3 );
			}
#endif
		}

		protected override bool CanDisposeBuffer()
		{
			return true;
		}
		#endregion

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
	[UnrealRegisterClass]
	public class UInterfaceProperty : UProperty
	{
		#region Serialized Members
		public UClass InterfaceObject;
		//public UInterfaceProperty InterfaceType = null;
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

			int index = _Buffer.ReadObjectIndex();
			InterfaceObject = (UClass)GetIndexObject( index );

			//Index = _Buffer.ReadObjectIndex();
			//_InterfaceType = (UInterfaceProperty)GetIndexObject( Index );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return InterfaceObject != null ? InterfaceObject.GetFriendlyType() : "@NULL";
		}
	}

	/// <summary>
	/// Delegate Property
	/// 
	/// UE2+
	/// </summary>
	[UnrealRegisterClass]
	public class UDelegateProperty : UProperty
	{
		#region Serialized Members
		public UObject FunctionObject;
		public UObject DelegateObject;
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
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
	public class UClassProperty : UObjectProperty
	{
		#region Serialized Members
		// MetaClass
		public UClass ClassObject;
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

			int classIndex = _Buffer.ReadObjectIndex();
			ClassObject = (UClass)GetIndexObject( classIndex );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			if( ClassObject != null )
			{
				return (String.Compare( ClassObject.Name, "Object", StringComparison.OrdinalIgnoreCase ) == 0) 
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
	[UnrealRegisterClass]
	public class UFixedArrayProperty : UProperty
	{
		#region Serialized Members
		public UProperty InnerObject;

		public int Count
		{
			get; 
			private set;
		}

		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UFixedArrayProperty class. 
		/// </summary>
		public UFixedArrayProperty()
		{
			Count = 0;
			Type = PropertyType.FixedArrayProperty;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int innerIndex = _Buffer.ReadObjectIndex();
			InnerObject = (UProperty)GetIndexObject( innerIndex );
			Count = _Buffer.ReadIndex();
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			// Just move to decompiling?
			return base.GetFriendlyType() + "[" + Count + "]";
		}
	}

	/// <summary>
	/// Dynamic Array Property
	/// </summary>
	[UnrealRegisterClass]
	public class UArrayProperty : UProperty
	{
		#region Serialized Members
		public UProperty InnerProperty;
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

			int innerIndex = _Buffer.ReadObjectIndex();
			InnerProperty = (UProperty)GetIndexObject( innerIndex );
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
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
	public class UStructProperty : UProperty
	{
		#region Serialized Members
		public UStruct StructObject;
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
			return StructObject != null ? StructObject.GetFriendlyType() : "@NULL";
		}
	}

	/// <summary>
	/// Byte Property
	/// </summary>
	[UnrealRegisterClass]
	public class UByteProperty : UProperty
	{
		#region Serialized Members
		public UEnum EnumObject;
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

			int enumIndex = _Buffer.ReadObjectIndex();
			EnumObject = (UEnum)GetIndexObject( enumIndex );
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

			var pkg = LoadImportPackage();
			if( pkg != null )
			{		
				if( pkg.Objects == null )
				{
					pkg.RegisterClass( "ByteProperty", typeof(UByteProperty) );
					pkg.RegisterClass( "Enum", typeof(UEnum) );
					pkg.InitializeExportObjects();
				}
				var b = (UByteProperty)pkg.FindObject( Name, typeof(UByteProperty) );
				if( b != null )
				{
					EnumObject = b.EnumObject;
				}
			}
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return EnumObject != null ? EnumObject.GetOuterGroup() : "byte";
		}
	}

	/// <summary>
	/// Int Property
	/// </summary>
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
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
	}

	/// <summary>
	/// Float Property
	/// </summary>
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
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
	[UnrealRegisterClass]
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

			int objectIndex = _Buffer.ReadObjectIndex();
			Object = GetIndexObject( objectIndex );
		}

		/// <inheritdoc/>
		public override string GetFriendlyType()
		{
			return Object != null ? Object.GetFriendlyType() : "@NULL";
		}
	}
}