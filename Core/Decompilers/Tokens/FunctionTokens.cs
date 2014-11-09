using System;
using System.Collections.Generic;
using System.Text;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class EndFunctionParmsToken : Token
            {
                public override string Decompile()
                {
                    return ")";
                }
            }

            public abstract class FunctionToken : Token
            {
                protected void DeserializeCall()
                {
                    DeserializeParms();
                    Decompiler.DeserializeDebugToken();
                }

                private void DeserializeParms()
                {
#pragma warning disable 642
                    while( !(DeserializeNext() is EndFunctionParmsToken) );
#pragma warning restore 642
                }

                protected void DeserializeBinaryOperator()
                {
                    DeserializeNext();
                    DeserializeNext();

                    DeserializeNext(); // )
                    Decompiler.DeserializeDebugToken();
                }

                protected void DeserializeUnaryOperator()
                {
                    DeserializeNext();

                    DeserializeNext(); // )
                    Decompiler.DeserializeDebugToken();
                }

                private static string PrecedenceToken( Token t )
                {
                    if( !(t is FunctionToken) )
                        return t.Decompile();

                    // Always add ( and ) unless the conditions below are not met, in case of a VirtualFunctionCall.
                    var addParenthesises = true;
                    if( t is NativeFunctionToken )
                    {
                        addParenthesises = ((NativeFunctionToken)t).NativeTable.Type == FunctionType.Operator;
                    }
                    else if( t is FinalFunctionToken )
                    {
                        addParenthesises = ((FinalFunctionToken)t).Function.IsOperator();
                    }
                    return addParenthesises ? String.Format( "({0})", t.Decompile() ) : t.Decompile();
                }

                protected string DecompilePreOperator( string operatorName )
                {
                    string output = operatorName + (operatorName.Length > 1 ? " " : String.Empty) + DecompileNext();
                    DecompileNext(); // )
                    return output;
                }

                protected string DecompileOperator( string operatorName )
                {
                    string output = String.Format( "{0} {1} {2}",
                        PrecedenceToken( GrabNextToken() ),
                        operatorName,
                        PrecedenceToken( GrabNextToken() )
                    );
                    DecompileNext(); // )
                    return output;
                }

                protected string DecompilePostOperator( string operatorName )
                {
                    string output = operatorName + " " + DecompileNext();
                    DecompileNext(); // )
                    return output;
                }

                protected string DecompileCall( string functionName )
                {
                    if( Decompiler._IsWithinClassContext )
                    {
                        functionName = "static." + functionName;

                        // Set false elsewhere as well but to be sure we set it to false here to avoid getting static calls inside the params.
                        // e.g.
                        // A1233343.DrawText(Class'BTClient_Interaction'.static.A1233332(static.Max(0, A1233328 - A1233322[A1233222].StartTime)), true);
                        Decompiler._IsWithinClassContext = false;
                    }
                    string output = functionName + "(" + DecompileParms();
                    return output;
                }

                private string DecompileParms()
                {
                    var tokens = new List<Tuple<Token, String>>();
                    {
                    next:
                        var t = GrabNextToken();
                        tokens.Add( Tuple.Create( t, t.Decompile() ) );
                        if( !(t is EndFunctionParmsToken) )
                            goto next;
                    }

                    var output = new StringBuilder();
                    for( int i = 0; i < tokens.Count; ++ i )
                    {
                        var t = tokens[i].Item1; // Token
                        var v = tokens[i].Item2; // Value

                        if( t is NoParmToken ) // Skipped optional parameters
                        {
                            output.Append( v );
                        }
                        else if( t is EndFunctionParmsToken ) // End ")"
                        {
                            output = new StringBuilder( output.ToString().TrimEnd( ',' ) + v );
                        }
                        else // Any passed values
                        {
                            if( i != tokens.Count - 1 && i > 0 ) // Skipped optional parameters
                            {
                                output.Append( v == String.Empty ? "," : ", " );
                            }
                            output.Append( v );
                        }
                    }
                    return output.ToString();
                }
            }

            public class FinalFunctionToken : FunctionToken
            {
                public UFunction Function;

                public override void Deserialize( IUnrealStream stream )
                {
                    if( stream.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(int) );
                    }

                    Function = stream.ReadObject() as UFunction;
                    Decompiler.AlignObjectSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    string output = String.Empty;
                    if( Function != null )
                    {
                        // Support for non native operators.
                        if( Function.IsPost() )
                        {
                            output = DecompilePreOperator( Function.FriendlyName );
                        }
                        else if( Function.IsPre() )
                        {
                            output = DecompilePostOperator( Function.FriendlyName );
                        }
                        else if( Function.IsOperator() )
                        {
                            output = DecompileOperator( Function.FriendlyName );
                        }
                        else
                        {
                            // Calling Super??.
                            if( Function.Name == Decompiler._Container.Name && !Decompiler._IsWithinClassContext )
                            {
                                output = "super";

                                // Check if the super call is within the super class of this functions outer(class)
                                var myouter = (UField)Decompiler._Container.Outer;
                                if( myouter == null || myouter.Super == null || Function.GetOuterName() != myouter.Super.Name  )
                                {
                                    // There's no super to call then do a recursive super call.
                                    if( Decompiler._Container.Super == null )
                                    {
                                        output += "(" + Decompiler._Container.GetOuterName() + ")";
                                    }
                                    else
                                    {
                                        // Different owners, then it is a deep super call.
                                        if( Function.GetOuterName() != Decompiler._Container.GetOuterName() )
                                        {
                                            output += "(" + Function.GetOuterName() + ")";
                                        }
                                    }
                                }
                                output += ".";
                            }
                            output += DecompileCall( Function.Name );
                        }
                    }
                    Decompiler._CanAddSemicolon = true;
                    return output;
                }
            }

            public class VirtualFunctionToken : FunctionToken
            {
                public int FunctionNameIndex;

                public override void Deserialize( IUnrealStream stream )
                {
                    // TODO: Corrigate Version (Definitely not in MOHA, but in roboblitz(369))
                    if( stream.Version >= 178 && stream.Version < 421/*MOHA*/ )
                    {
                        byte isSuperCall = stream.ReadByte();
                        Decompiler.AlignSize( sizeof(byte) );
                    }

                    if( stream.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(int) );
                    }

                    FunctionNameIndex = stream.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileCall( Package.GetIndexName( FunctionNameIndex ) );
                }
            }

            public class GlobalFunctionToken : FunctionToken
            {
                public int FunctionNameIndex;

                public override void Deserialize( IUnrealStream stream )
                {
                    FunctionNameIndex = stream.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return "global." + DecompileCall( Package.GetIndexName( FunctionNameIndex ) );
                }
            }

            public class DelegateFunctionToken : FunctionToken
            {
                public int FunctionNameIndex;

                public override void Deserialize( IUnrealStream stream )
                {
                    // TODO: Corrigate Version
                    if( stream.Version > 180 )
                    {
                        ++ stream.Position; // ReadByte()
                        Decompiler.AlignSize( sizeof(byte) );
                    }

                    // Delegate object index
                    stream.ReadObjectIndex();
                    Decompiler.AlignObjectSize();

                    // Delegate name index
                    FunctionNameIndex = stream.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileCall( Decompiler._Container.Package.GetIndexName( FunctionNameIndex ) );
                }
            }

            public class NativeFunctionToken : FunctionToken
            {
                public NativeTableItem NativeTable;

                public override void Deserialize( IUnrealStream stream )
                {
                    if( NativeTable == null )
                    {
                        NativeTable = new NativeTableItem
                        {
                            Type = FunctionType.Function,
                            Name = "UnresolvedNativeFunction_" + RepresentToken,
                            ByteToken = RepresentToken
                        };
                    }

                    switch( NativeTable.Type )
                    {
                        case FunctionType.Function:
                            DeserializeCall();
                            break;

                        case FunctionType.PreOperator:
                        case FunctionType.PostOperator:
                            DeserializeUnaryOperator();
                            break;

                        case FunctionType.Operator:
                            DeserializeBinaryOperator();
                            break;

                        default:
                            DeserializeCall();
                            break;
                    }
                }

                public override string Decompile()
                {
                    string output;
                    switch( NativeTable.Type )
                    {
                        case FunctionType.Function:
                            output = DecompileCall( NativeTable.Name );
                            break;

                        case FunctionType.Operator:
                            output = DecompileOperator( NativeTable.Name );
                            break;

                        case FunctionType.PostOperator:
                            output = DecompilePostOperator( NativeTable.Name );
                            break;

                        case FunctionType.PreOperator:
                            output = DecompilePreOperator( NativeTable.Name );
                            break;

                        default:
                            output = DecompileCall( NativeTable.Name );
                            break;
                    }
                    Decompiler._CanAddSemicolon = true;
                    return output;
                }
            }
        }
    }
}