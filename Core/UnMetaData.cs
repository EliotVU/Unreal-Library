/**
 *	UnMetaData.cs http://udn.epicgames.com/Three/UnrealScriptReference.html#UnrealScriptMetadata
 */
#define DEBUGUE3
#define DECUNIQUETAGS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib.Core
{
	/// <summary>
	/// Represents metadata from field(Properties,Enums,Structs and Constants) objects. 
	/// </summary>
	public sealed class UMetaData : UObject
	{
		#region Serialized Members
		public sealed class UMetaField : IUnrealDecompilable, IUnrealDeserializableClass
		{
			public int					FieldIndex;			// ObjectIndex		
			public string				FieldName;			// UT3, Mirrors Edge
			public UArray<UMetaTag> 	MetaTags;
			public UnrealPackage		Owner;

			public void Deserialize( IUnrealStream stream )
			{
				if( stream.Version <= 540 )
				{
					// e.g. Core.Object.X
					FieldName = stream.ReadName();
				}
				else 
				{
					FieldIndex = stream.ReadObjectIndex();
				}
				MetaTags = new UArray<UMetaTag>();
				MetaTags.Deserialize( stream, delegate( UMetaTag tag ){ tag.Owner = Owner; } );
			}

			/// <summary>
			/// Decompiles this object into a text format of:
			/// 
			/// LeftArrow
			///		"ForEach MetaTags"
			///		Tag.Decompile()|
			///	RightArrow
			/// </summary>
			/// <returns></returns>
			public string Decompile()
			{
				List<UMetaTag> tags = new List<UMetaTag>( MetaTags.Count );
 
				tags.AddRange( MetaTags );

				// We remove this because OrderIndex is a implicit added MetaTag.
				var OItag = GetMetaTag( "OrderIndex" );
				if( OItag != null )
				{
					tags.Remove( OItag );
				}

				OItag = GetMetaTag( "ToolTip" );
				if( OItag != null )
				{
					tags.Remove( OItag );
				}

				/*OItag = GetMetaTag( "ToolTip" );
				if( OItag != null )
				{
					tags.Remove( OItag );
				}*/

				if( tags != null && tags.Count > 0 )
				{
 					string output = "<";
					foreach( UMetaTag tag in tags )
					{
				   		output += tag.Decompile() + (tag != tags.Last() ? "|" : ">");
					}
					return output;
				}
				return String.Empty;
			}

			public string Decompile( ref string outer )
			{
				return outer + Decompile();
			}	
	
			public UMetaTag GetMetaTag( string tagName )
			{
				return MetaTags.Find( delegate( UMetaTag tag ){ return Owner.GetIndexName( tag.TagNameIndex ) == tagName; } );
			}
		}															    

		public sealed class UMetaTag : IUnrealDecompilable, IUnrealDeserializableClass
		{
			public int				TagNameIndex;
			public string 			TagValue;
			public UnrealPackage	Owner;

			public void Deserialize( IUnrealStream stream )
			{
				TagNameIndex = stream.ReadNameIndex();
				TagValue = stream.ReadName();
			}

			/// <summary>
			/// Decompiles this object into a text format of:
			/// 
			/// TagName=TagValue
			/// </summary>
			/// <returns></returns>
			public string Decompile()
			{								
				return Owner.GetIndexName( TagNameIndex ) + "=" + TagValue;
			}
		}

		private UArray<UMetaField> _MetaFields = null;
		#endregion

		protected override void Deserialize()
		{
			base.Deserialize();

			_MetaFields = new UArray<UMetaField>();
			_MetaFields.Deserialize( _Buffer, delegate( UMetaField field ){ field.Owner = Package; } );

			// Temp, for debugging the UE3 format.
			#if DEBUGUE3
			PostInitialize();
			#endif
		}

		public override void PostInitialize()
		{
			base.PostInitialize();

			// Link the metas to the owners
			foreach( UMetaField MetaProp in _MetaFields )
			{
				UField field = (UField)Package.GetIndexObject( MetaProp.FieldIndex );
				if( field != null )
				{
					field.Meta = MetaProp;
				}
			}
		}

		/// <summary>
		/// Decompiles this object into a text format of:
		/// 
		///	Meta Count _MetaFields.Count
		///		
		/// "ForEach _MetaFields"
		/// 
		///		fieldname+Field.Decompile()
		/// </summary>
		/// <returns></returns>
		public override string Decompile()
		{
			// UE3 Debug
			BeginDeserializing();

			if( _MetaFields == null )
			{
			    return "No MetaFields found!";
			}

			string content = FormatHeader();
			foreach( UMetaField field in _MetaFields )
			{
				string fieldname = field.FieldIndex != 0 ? GetIndexObject( field.FieldIndex ).GetOuterGroup() : field.FieldName;																			  // Max string length!
				string fieldOutput = field.Decompile();

				if( fieldOutput.Length != 0 )
				{
					content += "\r\n" + fieldname + field.Decompile();
				}
			}	
			return content;
		}

		protected override string FormatHeader()
		{
			if( _MetaFields == null )
			{
			    return "No MetaFields found!";
			}

			return "Meta count: " + _MetaFields.Count + "\r\n";
		}

		public string GetUniqueMetas()
		{
			string output = String.Empty;
			List<UMetaTag> tags = new List<UMetaTag>();
			foreach( UMetaField field in _MetaFields )
			{
				foreach( UMetaTag dfield in field.MetaTags )
				{
					UMetaTag ut = new UMetaTag();
					ut.Owner = dfield.Owner;
					if( tags.Find( delegate( UMetaTag tag ){ return tag.TagNameIndex == dfield.TagNameIndex; } ) != null )
					{
						continue;
					}			
					ut.TagNameIndex = dfield.TagNameIndex;

					ut.TagValue = field.FieldName;
					/*
					if( field.FieldIndex != 0 )
					{
						ut.TagValue = GetIndexObject( field.FieldIndex ).GetOuterGroup() + "." + dfield.TagValue;
					}
					else
					{
						ut.TagValue = field.FieldName + "." + dfield.TagValue;
					}*/
					tags.Add( ut );
				}
			}

			foreach( UMetaTag ut in tags )
			{
				string tagsOutput = ut.Decompile().TrimEnd( '=' );
				if( tagsOutput.Length != 0 )
				{
					output += tagsOutput + "\r\n";
				}
			}
			return output;
		}
	}
}
