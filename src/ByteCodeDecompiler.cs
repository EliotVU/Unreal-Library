﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UELib.Flags;
using UELib.Services;

namespace UELib.Core
{
    using System.Linq;
    using System.Text;

    public partial class UStruct
    {
        public partial class UByteCodeDecompiler(UStruct container, UByteCodeScript script) : IUnrealDecompilable
        {
            private readonly UStruct _Container = container;

            public readonly UnrealPackage Package = container.Package;
            public UByteCodeScript Script { get; } = script ?? throw new ArgumentNullException(nameof(script), "Script cannot be null.");

            public int CurrentTokenIndex { get; set; } = -1;
            public List<Token> DeserializedTokens => Script.Tokens;
            public Token NextToken => DeserializedTokens[++CurrentTokenIndex];
            public Token PeekToken => DeserializedTokens[CurrentTokenIndex + 1];
            public Token PreviousToken => DeserializedTokens[CurrentTokenIndex - 1];
            public Token CurrentToken => DeserializedTokens[CurrentTokenIndex];

            /// <summary>
            /// Context hint, a reference to the last relevant-object that was accessed during the decompilation.
            /// </summary>
            private UObject? _ObjectHint;

            public UByteCodeDecompiler(UStruct container) : this(
                container,
                // Only ever null if the container has no script, but create one anyway, so that this construction is always safe.
                container.Script ?? new UByteCodeScript(
                    container,
                    container.MemoryScriptSize,
                    container.StorageScriptSize))
            {
            }

            public void Deserialize()
            {
                if (Script.MemorySize == 0 || Script.Tokens.Count != 0)
                {
                    return;
                }

                try
                {
                    var buffer = _Container.LoadBuffer();
                    buffer.Seek(_Container.ScriptOffset, SeekOrigin.Begin);
                    Script.Deserialize(buffer);
                }
                finally
                {
                    _Container.MaybeDisposeBuffer();
                }
            }

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

                public readonly List<Nest> Nests = [];

                public void AddNest(Nest.NestType type, int position, int endPosition, Token? creator = null)
                {
                    creator = creator ?? Decompiler.CurrentToken;
                    Nests.Add(new NestBegin { Position = position, Type = type, Creator = creator });
                    Nests.Add(new NestEnd { Position = endPosition, Type = type, Creator = creator });
                }

                public NestBegin AddNestBegin(Nest.NestType type, int position, Token? creator = null)
                {
                    var n = new NestBegin { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public NestEnd AddNestEnd(Nest.NestType type, int position, Token? creator = null)
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
            public NestManager Nester => _Nester;

            // Checks if we're currently within a nest of type nestType in any stack!
            private NestManager.Nest? IsWithinNest(NestManager.Nest.NestType nestType)
            {
                for (int i = _NestChain.Count - 1; i >= 0; --i)
                    if (_NestChain[i].Type == nestType)
                        return _NestChain[i];

                return null;
            }

            // Checks if the current nest is of type nestType in the current stack!
            // Only BeginNests that have been decompiled will be tested for!
            private NestManager.Nest? IsInNest(NestManager.Nest.NestType nestType)
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

                _ObjectHint = null;

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

                _Labels = [];
                _TempLabels = new List<(ULabelEntry, int)>();

                foreach (var token in Script.Statements)
                {
                    switch (token)
                    {
                        // if ()
                        case JumpToken jumpToken when token is JumpIfNotToken jumpIfNotToken:
                            {
                                // Add jump label for 'do until' jump pattern
                                if ((jumpIfNotToken.CodeOffset & ushort.MaxValue) < jumpIfNotToken.Position)
                                {
                                    _Labels.Add
                                    (
                                        new ULabelEntry
                                        {
                                            Name = UDecompilingState.OffsetLabelName(jumpIfNotToken.CodeOffset),
                                            Position = jumpIfNotToken.CodeOffset
                                        }
                                    );
                                }

                                break;
                            }
                        // goto Label;
                        case JumpToken jumpToken:
                            {
                                if ((jumpToken.CodeOffset & ushort.MaxValue) < token.Position)
                                {
                                    _Labels.Add
                                    (
                                        new ULabelEntry
                                        {
                                            Name = UDecompilingState.OffsetLabelName(jumpToken.CodeOffset),
                                            Position = jumpToken.CodeOffset
                                        }
                                    );
                                }

                                break;
                            }
                        // State labels
                        case LabelTableToken labelTableToken:
                            // Add label for 'goto' jump pattern
                            _Labels.AddRange(labelTableToken.Labels);
                            break;
                    }

                    // Fallback for legacy tokens.
                    token.PostDeserialized();
                }

                for (var i = 0; i < _Labels.Count; ++i)
                {
                    // No duplicates, caused by having multiple goto statements with the same destination
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

            public void PopSemicolon()
            {
                _CanAddSemicolon = false;
            }

            public void MarkCommentStatement()
            {
                _MustCommentStatement = true;
            }

            public string Decompile()
            {
#if !DECOMPILE
                return string.Empty;
#else
                // Ensure we have deserialized tokens to work with.
                Deserialize();

                if (DeserializedTokens.Count == 0)
                {
                    return string.Empty;
                }

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
                        foreach (var parm in func.EnumerateFields<UProperty>().Where(field => field.IsParm()))
                        {
                            if (!parm.PropertyFlags.HasFlag(PropertyFlag.OptionalParm))
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
                                        } while (CurrentToken is not EndParmValueToken);

                                        ++CurrentTokenIndex; // EndParmValueToken
                                        break;
                                    }

                                default:
                                    // Can be true e.g a function with optionals but no default values
                                    //Debug.Fail($"Unexpected token for optional parameter {parm.GetOuterGroup()}");
                                    break;
                            }
                        }

                        // The decompiler expects to start with a -1 index
                        --CurrentTokenIndex;
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
                            catch (Exception exception)
                            {
                                output.Append($"// ({exception.GetType().Name})");
                                LibServices.LogService.SilentException(exception);
                            }

                            try
                            {
                                tokenOutput = newToken.Decompile(this);
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
                            catch (Exception exception)
                            {
                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs + "/* Statement decompilation error: "
                                                  + exception.Message);

                                UDecompilingState.AddTab();
                                string tokensOutput = FormatAndDecompileTokens(tokenBeginIndex, tokenEndIndex);
                                output.Append(UDecompilingState.Tabs);
                                output.Append(tokensOutput);
                                UDecompilingState.RemoveTab();

                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs
                                                  + "*/");

                                tokenOutput = "/*@Error*/";

                                LibServices.LogService.SilentException(exception);
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
                        catch (Exception exception)
                        {
                            output.Append("\r\n" + UDecompilingState.Tabs + "// Failed to format nests!:"
                                          + exception + "\r\n"
                                          + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                          + _Nester.Nests[_Nester.Nests.Count - 1]);
                            spewOutput = true;

                            LibServices.LogService.SilentException(exception);
                        }
                    }

                    try
                    {
                        // Decompile remaining nests
                        output.Append(DecompileNests(true));
                    }
                    catch (Exception exception)
                    {
                        output.Append("\r\n" + UDecompilingState.Tabs
                                             + "// Failed to format remaining nests!:" + exception + "\r\n"
                                             + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                             + _Nester.Nests[_Nester.Nests.Count - 1]);

                        LibServices.LogService.SilentException(exception);
                    }
                }
                catch (Exception exception)
                {
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine("// Cannot recover from this decompilation error.");
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine($"// Error: {FormatTabs(exception.Message)}");
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine($"// Token Index: {CurrentTokenIndex} / {DeserializedTokens.Count}");

                    LibServices.LogService.SilentException(exception);
                }
                finally
                {
                    UDecompilingState.Tabs = initTabs;
                }

                return output.ToString();
#endif
            }

            private readonly List<NestManager.Nest> _NestChain = [];

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
                                  $"{FormatTokenInfo(t)} << {t.Decompile(this)}";
                    }
                    catch (Exception exception)
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {exception.GetType().FullName}";
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

                        LibServices.LogService.SilentException(exception);
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
                bool isStateLabel = !labelEntry.Name.ToString().StartsWith("J0x", StringComparison.Ordinal);
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
                    if (_Nester.Nests[i] is not NestManager.NestBegin)
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
        }
    }
}
