using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core.Tokens;
using UELib.Flags;
using UELib.Tokens;

namespace UELib.Core
{
    using System.Linq;
    using System.Text;
    using UELib.Branch.UE3.RSS.Tokens;

    public partial class UStruct
    {
        public partial class UByteCodeDecompiler : IUnrealDecompilable
        {
            private readonly UStruct _Container;
            private UObjectStream _Buffer;
            private readonly UnrealPackage _Package;
            private readonly TokenFactory _TokenFactory;

            /// <summary>
            /// A collection of deserialized tokens, in their correspondence stream order.
            /// </summary>
            public List<Token> DeserializedTokens { get; private set; }

            [System.ComponentModel.DefaultValue(-1)]
            public int CurrentTokenIndex { get; set; }

            public Token NextToken => DeserializedTokens[++CurrentTokenIndex];

            public Token PeekToken => DeserializedTokens[CurrentTokenIndex + 1];

            public Token PreviousToken => DeserializedTokens[CurrentTokenIndex - 1];

            public Token CurrentToken => DeserializedTokens[CurrentTokenIndex];

            public UByteCodeDecompiler(UStruct container)
            {
                _Container = container;
                
                _Package = container.Package;
                Debug.Assert(_Package != null);
                _TokenFactory = container.GetTokenFactory();
                Debug.Assert(_TokenFactory != null);
                
                SetupMemorySizes();
            }

            #region Deserialize

            /// <summary>
            /// The current in memory position relative to the first byte-token.
            /// </summary>
            private int ScriptPosition { get; set; }

            /// <summary>
            /// Size of FName in memory (int Index, (>= 343) int Number).
            /// </summary>
            private byte _NameMemorySize = sizeof(int);

            /// <summary>
            /// Size of a pointer to an UObject in memory.
            /// 32bit, 64bit as of version 587 (even on 32bit platforms)
            /// </summary>
            private byte _ObjectMemorySize = sizeof(int);

            private void SetupMemorySizes()
            {
#if BIOSHOCK
                if (_Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                {
                    _NameMemorySize = sizeof(int) + sizeof(int);
                    return;
                }
#endif
                const uint vNameSizeTo8 = (uint)PackageObjectLegacyVersion.NumberAddedToName;
                if (_Package.Version >= vNameSizeTo8) _NameMemorySize = sizeof(int) + sizeof(int);
#if TERA
                // Tera's reported version is false (partial upgrade?)
                if (_Package.Build == UnrealPackage.GameBuild.BuildName.Tera) return;
#endif
                const short vObjectSizeTo8 = 587;
                if (_Package.Version >= vObjectSizeTo8) _ObjectMemorySize = sizeof(long);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void AlignSize(int size)
            {
                ScriptPosition += size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void AlignNameSize()
            {
                ScriptPosition += _NameMemorySize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void AlignObjectSize()
            {
                ScriptPosition += _ObjectMemorySize;
            }

            private bool _WasDeserialized;

            public void Deserialize()
            {
                if (_WasDeserialized)
                    return;

                _WasDeserialized = true;
                CurrentTokenIndex = -1;
                DeserializedTokens = new List<Token>();
                _Labels = new List<ULabelEntry>();
                ScriptPosition = 0;
                try
                {
                    _Container.EnsureBuffer();
                    _Buffer = _Container.Buffer;
                    _Buffer.Seek(_Container.ScriptOffset, SeekOrigin.Begin);
                    int scriptSize = _Container.ByteScriptSize;
                    while (ScriptPosition < scriptSize)
                        try
                        {
                            DeserializeNext();
                        }
                        catch (EndOfStreamException error)
                        {
                            Console.Error.WriteLine("Couldn't backup from this error! Decompiling aborted!");
                            break;
                        }
                        catch (SystemException e)
                        {
                            Console.WriteLine("Object:" + _Container.Name);
                            Console.WriteLine("Failed to deserialize token at position:" + ScriptPosition);
                            Console.WriteLine("Exception:" + e.Message);
                            Console.WriteLine("Stack:" + e.StackTrace);
                        }
                }
                finally
                {
                    _Container.MaybeDisposeBuffer();
                }
            }

            private void DeserializeDebugToken()
            {
                // Sometimes we may end up at the end of a script
                // -- and by coincidence pickup a DebugInfo byte-code outside of the script-boundary.
                if (ScriptPosition+sizeof(byte)+sizeof(int) >= _Container.ByteScriptSize) return;

                long p = _Buffer.Position;
                byte opCode = _Buffer.ReadByte();

                // Let's avoid converting native calls to a token type ;D
                if (opCode >= _TokenFactory.ExtendedNative)
                {
                    _Buffer.Position = p;
                    return;
                }

                int version = 0;
                
                var tokenType = _TokenFactory.GetTokenTypeFromOpCode(opCode);
                if (tokenType == typeof(DebugInfoToken))
                {
                    // Sometimes we may catch a false positive,
                    // e.g. A FinalFunction within an Iterator may expect a debug token and by mere coincidence match the Iterator's CodeOffset.
                    // So let's verify the next 4 bytes too.
                    version = _Buffer.ReadInt32();
                }

                _Buffer.Position = p;
                if (version == 100)
                {
                    // Expecting a DebugInfo token.
                    DeserializeNext();
                }
            }

            private Token DeserializeNextOpCodeToToken()
            {
                byte opCode = _Buffer.ReadByte();
                AlignSize(sizeof(byte));

                if (opCode < _TokenFactory.ExtendedNative) return _TokenFactory.CreateToken<Token>(opCode);
                if (opCode >= _TokenFactory.FirstNative)
                {
                    return _TokenFactory.CreateNativeToken(opCode);
                }

                byte opCodeExtension = _Buffer.ReadByte();
                AlignSize(sizeof(byte));

                var nativeIndex = (ushort)(((opCode - _TokenFactory.ExtendedNative) << 8) | opCodeExtension);
                Debug.Assert(nativeIndex < (ushort)ExprToken.MaxNative);
                return _TokenFactory.CreateNativeToken(nativeIndex);
            }

            private Token DeserializeNext()
            {
                int scriptPosition = ScriptPosition;
                var token = DeserializeNextOpCodeToToken();
                Debug.Assert(token != null);

                DeserializedTokens.Add(token);
                token.Decompiler = this;
                token.Position = scriptPosition;
                token.StoragePosition = (int)(_Buffer.Position - _Container.ScriptOffset - 1);
                token.Deserialize(_Buffer);
                token.Size = (short)(ScriptPosition - scriptPosition);
                token.StorageSize = (short)(_Buffer.Position - _Container.ScriptOffset - token.StoragePosition);
                token.PostDeserialized();
                
                return token;
            }
            #endregion

#if DECOMPILE

#region Decompile

            public class NestManager
            {
                public UByteCodeDecompiler Decompiler;

                public class Nest : IUnrealDecompilable
                {
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
                        "CA1008:EnumsShouldHaveZeroValue")]
                    public enum NestType : byte
                    {
                        Scope = 0,
                        If = 1,
                        Else = 2,
                        ForEach = 4,
                        Switch = 5,
                        Case = 6,
                        Default = 7,
                        Loop = 8
                    }

                    /// <summary>
                    /// Position of this Nest (CodePosition)
                    /// </summary>
                    public int Position;

                    public NestType Type;
                    public Token Creator;

                    public virtual string Decompile()
                    {
                        return string.Empty;
                    }

                    public bool IsPastOffset(int position)
                    {
                        return position >= Position;
                    }

                    public override string ToString()
                    {
                        return $"Type:{Type} Position:0x{Position:X3}";
                    }
                }

                public class NestBegin : Nest
                {
                    public override string Decompile()
                    {
#if DEBUG_NESTS
                        return "\r\n" + UDecompilingState.Tabs + "//<" + Type + ">";
#else
                        return Type != NestType.Case && Type != NestType.Default
                            ? UnrealConfig.PrintBeginBracket()
                            : string.Empty;
#endif
                    }
                }

                public class NestEnd : Nest
                {
                    public JumpToken HasElseNest;

                    public override string Decompile()
                    {
#if DEBUG_NESTS
                        return "\r\n" + UDecompilingState.Tabs + "//</" + Type + ">";
#else
                        return Type != NestType.Case && Type != NestType.Default
                            ? UnrealConfig.PrintEndBracket()
                            : string.Empty;
#endif
                    }
                }

                public readonly List<Nest> Nests = new List<Nest>();

                public void AddNest(Nest.NestType type, int position, int endPosition, Token creator = null)
                {
                    creator = creator ?? Decompiler.CurrentToken;
                    Nests.Add(new NestBegin { Position = position, Type = type, Creator = creator });
                    Nests.Add(new NestEnd { Position = endPosition, Type = type, Creator = creator });
                }

                public NestBegin AddNestBegin(Nest.NestType type, int position, Token creator = null)
                {
                    var n = new NestBegin { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public NestEnd AddNestEnd(Nest.NestType type, int position, Token creator = null)
                {
                    var n = new NestEnd { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public bool TryAddNestEnd(Nest.NestType type, int pos)
                {
                    foreach (var nest in Decompiler._Nester.Nests)
                        if (nest.Type == type && nest.Position == pos)
                            return false;

                    Decompiler._Nester.AddNestEnd(type, pos);
                    return true;
                }
            }

            private NestManager _Nester;

            // Checks if we're currently within a nest of type nestType in any stack!
            private NestManager.Nest IsWithinNest(NestManager.Nest.NestType nestType)
            {
                for (int i = _NestChain.Count - 1; i >= 0; --i)
                    if (_NestChain[i].Type == nestType)
                        return _NestChain[i];

                return null;
            }

            // Checks if the current nest is of type nestType in the current stack!
            // Only BeginNests that have been decompiled will be tested for!
            private NestManager.Nest IsInNest(NestManager.Nest.NestType nestType)
            {
                int i = _NestChain.Count - 1;
                if (i == -1)
                    return null;

                return _NestChain[i].Type == nestType ? _NestChain[i] : null;
            }

            public void InitDecompile()
            {
                _NestChain.Clear();

                _Nester = new NestManager { Decompiler = this };
                CurrentTokenIndex = -1;
                ScriptPosition = 0;

                FieldToken.LastField = null;

                // Reset these, in case of a loop in the Decompile function that did not finish due exception errors!
                _IsWithinClassContext = false;
                _CanAddSemicolon = false;
                _MustCommentStatement = false;
                _PostIncrementTabs = 0;
                _PostDecrementTabs = 0;
                _PreIncrementTabs = 0;
                _PreDecrementTabs = 0;
                PreComment = string.Empty;
                PostComment = string.Empty;

                _TempLabels = new List<(ULabelEntry, int)>();
                if (_Labels != null)
                    for (var i = 0; i < _Labels.Count; ++i)
                    {
                        // No duplicates, caused by having multiple goto's with the same destination
                        int index = _TempLabels.FindIndex(p => p.entry.Position == _Labels[i].Position);
                        if (index == -1)
                        {
                            _TempLabels.Add((_Labels[i], 1));
                        }
                        else
                        {
                            var data = _TempLabels[index];
                            data.refs++;
                            _TempLabels[index] = data;
                        }
                    }
            }

            public void JumpTo(ushort codeOffset)
            {
                int index = DeserializedTokens.FindIndex(t => t.Position == codeOffset);
                if (index == -1)
                    return;

                CurrentTokenIndex = index;
            }

            public Token TokenAt(ushort codeOffset)
            {
                return DeserializedTokens.Find(t => t.Position == codeOffset);
            }

            /// <summary>
            /// True if we are currently decompiling within a ClassContext token.
            ///
            /// HACK: For static calls -> class'ClassA'.static.FuncA();
            /// </summary>
            private bool _IsWithinClassContext;

            private bool _CanAddSemicolon;
            private bool _MustCommentStatement;

            private byte _PostIncrementTabs;
            private byte _PostDecrementTabs;
            private byte _PreIncrementTabs;
            private byte _PreDecrementTabs;

            public string PreComment;
            public string PostComment;

            public void MarkSemicolon()
            {
                _CanAddSemicolon = true;
            }

            public string Decompile()
            {
                // Make sure that everything is deserialized!
                if (!_WasDeserialized) Deserialize();

                var output = new StringBuilder();
                // Original indention, so that we can restore it later, necessary if decompilation fails to reduce nesting indention.
                string initTabs = UDecompilingState.Tabs;

#if DEBUG_TOKENPOSITIONS
                UDecompilingState.AddTabs(3);
#endif
                try
                {
                    //Initialize==========
                    InitDecompile();
                    var spewOutput = false;
                    var tokenEndIndex = 0;
                    Token lastStatementToken = null;
#if !DEBUG_HIDDENTOKENS
                    if (_Container is UFunction func
                        && func.HasOptionalParamData()
                        && DeserializedTokens.Count > 0)
                    {
                        CurrentTokenIndex = 0;
                        foreach (var parm in func.Params)
                        {
                            if (!parm.HasPropertyFlag(PropertyFlagsLO.OptionalParm))
                                continue;

                            // Skip NothingToken (No default value) and DefaultParameterToken (up to EndParmValueToken)
                            switch (CurrentToken)
                            {
                                case NothingToken _:
                                    ++CurrentTokenIndex; // NothingToken
                                    break;

                                case DefaultParameterToken _:
                                {
                                    do
                                    {
                                        ++CurrentTokenIndex;
                                    } while (!(CurrentToken is EndParmValueToken));

                                    ++CurrentTokenIndex; // EndParmValueToken
                                    break;
                                }

                                default:
                                    // Can be true e.g a function with optionals but no default values
                                    //Debug.Fail($"Unexpected token for optional parameter {parm.GetOuterGroup()}");
                                    break;
                            }
                        }
                    }
#endif
                    while (CurrentTokenIndex + 1 < DeserializedTokens.Count)
                    {
                        //Decompile chain==========
                        {
                            string tokenOutput;
                            var newToken = NextToken;
                            int tokenBeginIndex = CurrentTokenIndex;

                            // To ensure we print generated labels within a nesting block.
                            string labelsOutput = DecompileLabelForToken(CurrentToken, spewOutput);
                            if (labelsOutput != string.Empty) output.AppendLine(labelsOutput);

                            try
                            {
                                // FIX: Formatting issue on debug-compiled packages
                                if (newToken is DebugInfoToken)
                                {
                                    string nestsOutput = DecompileNests();
                                    if (nestsOutput != string.Empty)
                                    {
                                        output.Append(nestsOutput);
                                        spewOutput = true;
                                    }

#if !DEBUG_HIDDENTOKENS
                                    continue;
#endif
                                }
                            }
                            catch (EndOfStreamException)
                            {
                                break;
                            }
                            catch (Exception e)
                            {
                                output.Append($"// ({e.GetType().Name})");
                            }

                            try
                            {
                                tokenOutput = newToken.Decompile();
                                if (CurrentTokenIndex + 1 < DeserializedTokens.Count &&
                                    PeekToken is EndOfScriptToken)
                                {
                                    var firstToken = newToken is DebugInfoToken ? lastStatementToken : newToken;
                                    if (firstToken is ReturnToken)
                                    {
                                        var lastToken = newToken is DebugInfoToken ? PreviousToken : CurrentToken;
                                        if (lastToken is NothingToken || lastToken is ReturnNothingToken)
                                            _MustCommentStatement = true;
                                    }
                                }
                            }
                            catch (EndOfStreamException)
                            {
                                break;
                            }
                            catch (Exception e)
                            {
                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs + "/* Statement decompilation error: "
                                                  + e.Message);

                                UDecompilingState.AddTab();
                                string tokensOutput = FormatAndDecompileTokens(tokenBeginIndex, tokenEndIndex);
                                output.Append(UDecompilingState.Tabs);
                                output.Append(tokensOutput);
                                UDecompilingState.RemoveTab();

                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs
                                                  + "*/");

                                tokenOutput = "/*@Error*/";
                            }
                            finally
                            {
                                tokenEndIndex = CurrentTokenIndex;
                            }

                            // HACK: for multiple cases for one block of code, etc!
                            if (_PreDecrementTabs > 0)
                            {
                                UDecompilingState.RemoveTabs(_PreDecrementTabs);
                                _PreDecrementTabs = 0;
                            }

                            if (_PreIncrementTabs > 0)
                            {
                                UDecompilingState.AddTabs(_PreIncrementTabs);
                                _PreIncrementTabs = 0;
                            }

                            if (_MustCommentStatement && UnrealConfig.SuppressComments)
                                continue;

                            if (!UnrealConfig.SuppressComments)
                            {
                                if (PreComment != string.Empty)
                                {
                                    tokenOutput = PreComment + (string.IsNullOrEmpty(tokenOutput)
                                        ? tokenOutput
                                        : "\r\n" + UDecompilingState.Tabs + tokenOutput);

                                    PreComment = string.Empty;
                                }

                                if (PostComment != string.Empty)
                                {
                                    tokenOutput += PostComment;
                                    PostComment = string.Empty;
                                }
                            }

                            //Preprocess output==========
                            {
#if DEBUG_HIDDENTOKENS
                                if (tokenOutput.Length == 0) tokenOutput = ";";
#endif
#if DEBUG_TOKENPOSITIONS
#endif
                                // Previous did spew and this one spews? then a new line is required!
                                if (tokenOutput != string.Empty)
                                {
                                    // Spew before?
                                    if (spewOutput)
                                        output.Append("\r\n");
                                    else spewOutput = true;
                                }

#if DEBUG_TOKENPOSITIONS
                                string orgTabs = UDecompilingState.Tabs;
                                int spaces = Math.Max(3 * UnrealConfig.Indention.Length, 4);
                                UDecompilingState.RemoveSpaces(spaces);

                                var tokens = DeserializedTokens
                                    .GetRange(tokenBeginIndex, tokenEndIndex - tokenBeginIndex + 1)
                                    .Select(FormatTokenInfo);
                                string tokensInfo = string.Join(", ", tokens);

                                output.Append(UDecompilingState.Tabs);
                                output.AppendLine($"  [{tokensInfo}] ");
                                output.Append(UDecompilingState.Tabs);
                                output.Append(
                                    $"  (+{newToken.Position:X3}  {CurrentToken.Position + CurrentToken.Size:X3}) ");
#endif
                                if (spewOutput)
                                {
                                    if (_MustCommentStatement)
                                    {
                                        output.Append(UDecompilingState.Tabs);
                                        output.Append($"//{tokenOutput}");
                                        _MustCommentStatement = false;
                                    }
                                    else
                                    {
                                        output.Append(UDecompilingState.Tabs);
                                        output.Append(tokenOutput);
                                    }

                                    // One of the decompiled tokens wanted to be ended.
                                    if (_CanAddSemicolon)
                                    {
                                        output.Append(";");
                                        _CanAddSemicolon = false;
                                    }
                                }
#if DEBUG_TOKENPOSITIONS
                                UDecompilingState.Tabs = orgTabs;
#endif
                            }
                            lastStatementToken = newToken;
                        }

                        //Postprocess output==========
                        if (_PostDecrementTabs > 0)
                        {
                            UDecompilingState.RemoveTabs(_PostDecrementTabs);
                            _PostDecrementTabs = 0;
                        }

                        if (_PostIncrementTabs > 0)
                        {
                            UDecompilingState.AddTabs(_PostIncrementTabs);
                            _PostIncrementTabs = 0;
                        }

                        try
                        {
                            string nestsOutput = DecompileNests();
                            if (nestsOutput != string.Empty)
                            {
                                output.Append(nestsOutput);
                                spewOutput = true;
                            }
                        }
                        catch (Exception e)
                        {
                            output.Append("\r\n" + UDecompilingState.Tabs + "// Failed to format nests!:"
                                          + e + "\r\n"
                                          + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                          + _Nester.Nests[_Nester.Nests.Count - 1]);
                            spewOutput = true;
                        }
                    }

                    try
                    {
                        // Decompile remaining nests
                        output.Append(DecompileNests(true));
                    }
                    catch (Exception e)
                    {
                        output.Append("\r\n" + UDecompilingState.Tabs
                                             + "// Failed to format remaining nests!:" + e + "\r\n"
                                             + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                             + _Nester.Nests[_Nester.Nests.Count - 1]);
                    }
                }
                catch (Exception e)
                {
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine("// Cannot recover from this decompilation error.");
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine($"// Error: {FormatTabs(e.Message)}");
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine($"// Token Index: {CurrentTokenIndex} / {DeserializedTokens.Count}");
                }
                finally
                {
                    UDecompilingState.Tabs = initTabs;
                }

                return output.ToString();
            }

            private readonly List<NestManager.Nest> _NestChain = new List<NestManager.Nest>();

            private static string FormatTabs(string nonTabbedText)
            {
                return nonTabbedText.Replace("\n", "\n" + UDecompilingState.Tabs);
            }

            public static string FormatTokenInfo(Token token)
            {
                Debug.Assert(token != null);
                return $"{token.GetType().Name} (0x{token.OpCode:X2})";
            }

            private string FormatAndDecompileTokens(int beginIndex, int endIndex)
            {
                var output = string.Empty;
                for (int i = beginIndex; i < endIndex && i < DeserializedTokens.Count; ++i)
                {
                    var t = DeserializedTokens[i];
                    try
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {t.Decompile()}";
                    }
                    catch (Exception e)
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {e.GetType().FullName}";
                        try
                        {
                            output += "\r\n" + UDecompilingState.Tabs + "(";
                            UDecompilingState.AddTab();
                            string inlinedTokens = FormatAndDecompileTokens(i + 1, CurrentTokenIndex);
                            UDecompilingState.RemoveTab();
                            output += inlinedTokens
                                      + "\r\n" + UDecompilingState.Tabs
                                      + ")";
                        }
                        finally
                        {
                            i += CurrentTokenIndex - beginIndex;
                        }
                    }
                }

                return output;
            }

            private string DecompileLabelForToken(Token token, bool appendNewline)
            {
                var output = new StringBuilder();
                int labelIndex = _TempLabels.FindIndex((l) => l.entry.Position == token.Position);
                if (labelIndex == -1) return string.Empty;

                var labelEntry = _TempLabels[labelIndex].entry;
                bool isStateLabel = !labelEntry.Name.StartsWith("J0x", StringComparison.Ordinal);
                string statementOutput = isStateLabel
                    ? $"{labelEntry.Name}:\r\n"
                    : $"{UDecompilingState.Tabs}{labelEntry.Name}:";
                if (appendNewline) output.Append("\r\n");

                output.Append(statementOutput);

                _TempLabels.RemoveAt(labelIndex);
                return output.ToString();
            }

            private string DecompileNests(bool outputAllRemainingNests = false)
            {
                var output = string.Empty;

                // Give { priority hence separated loops
                for (var i = 0; i < _Nester.Nests.Count; ++i)
                {
                    if (!(_Nester.Nests[i] is NestManager.NestBegin))
                        continue;

                    if (_Nester.Nests[i].IsPastOffset((int)CurrentToken.Position) || outputAllRemainingNests)
                    {
                        output += _Nester.Nests[i].Decompile();
                        UDecompilingState.AddTab();

                        _NestChain.Add(_Nester.Nests[i]);
                        _Nester.Nests.RemoveAt(i--);
                    }
                }

                for (int i = _Nester.Nests.Count - 1; i >= 0; i--)
                    if (_Nester.Nests[i] is NestManager.NestEnd nestEnd
                        && (outputAllRemainingNests ||
                            nestEnd.IsPastOffset((int)CurrentToken.Position + CurrentToken.Size)))
                    {
                        var topOfStack = _NestChain[_NestChain.Count - 1];
                        if (topOfStack.Type == NestManager.Nest.NestType.Default &&
                            nestEnd.Type != NestManager.Nest.NestType.Default)
                        {
                            // Automatically close default when one of its outer nest closes
                            output += $"\r\n{UDecompilingState.Tabs}break;";
                            UDecompilingState.RemoveTab();
                            _NestChain.RemoveAt(_NestChain.Count - 1);

                            // We closed off the last case, it's safe to close of the switch as well
                            if (nestEnd.Type != NestManager.Nest.NestType.Switch)
                            {
                                var switchScope = _NestChain[_NestChain.Count - 1];
                                if (switchScope.Type == NestManager.Nest.NestType.Switch)
                                {
                                    output += $"\r\n{UDecompilingState.Tabs}}}";
                                    UDecompilingState.RemoveTab();
                                    _NestChain.RemoveAt(_NestChain.Count - 1);
                                }
                                else
                                {
                                    output += $"/* Tried to find Switch scope, found {switchScope.Type} instead */";
                                }
                            }
                        }

                        UDecompilingState.RemoveTab();
                        output += nestEnd.Decompile();

                        topOfStack = _NestChain[_NestChain.Count - 1];
                        if (topOfStack.Type != nestEnd.Type)
                            output += $"/* !MISMATCHING REMOVE, tried {nestEnd.Type} got {topOfStack}! */";
                        _NestChain.RemoveAt(_NestChain.Count - 1);

                        _Nester.Nests.RemoveAt(i);
                        if (nestEnd.HasElseNest != null)
                        {
                            output += $"\r\n{UDecompilingState.Tabs}else{UnrealConfig.PrintBeginBracket()}";
                            UDecompilingState.AddTab();
                            var begin = new NestManager.NestBegin
                            {
                                Type = NestManager.Nest.NestType.Else,
                                Creator = nestEnd.HasElseNest,
                                Position = nestEnd.Position
                            };
                            var end = new NestManager.NestEnd
                            {
                                Type = NestManager.Nest.NestType.Else,
                                Creator = nestEnd.HasElseNest,
                                Position = nestEnd.HasElseNest.CodeOffset
                            };
                            _Nester.Nests.Add(end);
                            _NestChain.Add(begin);
                        }
                    }

                return output;
            }

#endregion

#region Disassemble

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            public string Disassemble()
            {
                return string.Empty;
            }

#endregion

#endif
        }
    }
}
