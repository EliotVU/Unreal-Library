﻿using System.Windows.Forms;

namespace UELib.JsonDecompiler.Core
{
    public partial class UFunction
    {
        protected override void InitNodes( TreeNode node )
        {
            node.ToolTipText = FormatHeader();
            _ParentNode = AddSectionNode( node, typeof(UFunction).Name );

            var funcFlagsNode = AddTextNode( _ParentNode, "FunctionFlags:" + UnrealMethods.FlagToString( FunctionFlags ) );
            funcFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.FunctionFlags), FunctionFlags ) );

            if( RepOffset > 0 )
            {
                AddTextNode( _ParentNode, "Replication Offset:" + RepOffset );
            }
            base.InitNodes( _ParentNode );
        }

        protected override void AddChildren( TreeNode node )
        {
            base.AddChildren( node );
            AddObjectListNode( node, "Parameters", Params, "UProperty" );
            AddObjectListNode( node, "Locals", Locals, "UProperty" );
        }

        public override string GetImageName()
        {
            var name = string.Empty;
            if( HasFunctionFlag( Flags.FunctionFlags.Event ) )
            {
                name = "Event";
            }
            else if( HasFunctionFlag( Flags.FunctionFlags.Delegate ) )
            {
                name = "Delegate";
            }
            else if( HasFunctionFlag( Flags.FunctionFlags.Operator ) )
            {
                name = "Operator";
            }

            if( name != string.Empty )
            {
                if( IsPrivate() )
                {
                    return name + "-Private";
                }
                else if( IsProtected() )
                {
                    return name + "-Protected";
                }
                return name;
            }
            return base.GetImageName();
        }
    }
}