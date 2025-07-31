using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.IO;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.Return)]
            public class ReturnToken : Token
            {
                public Token? ReturnExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    if (stream.Version < (uint)PackageObjectLegacyVersion.ReturnExpressionAddedToReturnToken)
                    {
                        return;
                    }

                    ReturnExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    if (stream.Version < (uint)PackageObjectLegacyVersion.ReturnExpressionAddedToReturnToken)
                    {
                        return;
                    }

                    Contract.Assert(ReturnExpression != null);
                    Script.SerializeToken(stream, ReturnExpression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    // HACK: for case's that end with a return instead of a break.
                    if (decompiler.IsInNest(NestManager.Nest.NestType.Default) != null)
                    {
                        decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, Position + Size);
                    }

                    decompiler.MarkSemicolon();

                    if (ReturnExpression == null)
                    {
                        return "return";
                    }

                    string returnValue = DecompileNext(decompiler);
                    return "return" + (returnValue.Length != 0
                        ? " " + returnValue
                        : string.Empty);

                    // FIXME: Transport the emitted "ReturnValue = Expression" over here.
                }
            }

            [ExprToken(ExprToken.ReturnNothing)]
            public class ReturnNothingToken : EatReturnValueToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    // HACK: for case's that end with a return instead of a break.
                    if (decompiler.IsInNest(NestManager.Nest.NestType.Default) != null)
                    {
                        decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, Position + Size);
                    }

                    return ReturnValueProperty?.Name ?? string.Empty;
                }
            }

            [ExprToken(ExprToken.GotoLabel)]
            public class GotoLabelToken : Token
            {
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return $"goto {DecompileNext(decompiler)}";
                }
            }

            [ExprToken(ExprToken.Jump)]
            public class JumpToken : Token
            {
                public bool MarkedAsSwitchBreak;
                public NestManager.NestEnd? LinkedIfNest;
                public ushort CodeOffset;

                public override void Deserialize(IUnrealStream stream)
                {
#if UE4
                    if (stream.IsUE4())
                    {
                        CodeOffset = (ushort)stream.ReadInt32();
                        Script.AlignSize(sizeof(int));

                        return;
                    }
#endif
                    CodeOffset = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));
                }

                public override void Serialize(IUnrealStream stream)
                {
#if UE4
                    if (stream.IsUE4())
                    {
                        stream.Write((uint)CodeOffset);
                        Script.AlignSize(sizeof(int));

                        return;
                    }
#endif
                    stream.Write((ushort)CodeOffset);
                    Script.AlignSize(sizeof(ushort));
                }

                protected void SetEndComment(UByteCodeDecompiler decompiler)
                {
                    decompiler.PreComment = $"// End:0x{CodeOffset:X2}";
                }

                private void SetStatementComment(UByteCodeDecompiler decompiler, string statement)
                {
                    decompiler.PreComment = $"// [{statement}]";
                }

                /// <summary>
                /// FORMATION ISSUESSES:
                ///     1:(-> Logic remains the same)   (Continue) statements are decompiled to (Else) statements e.g.
                ///         -> Original
                ///         if( continueCondition )
                ///         {
                ///             continue;
                ///         }
                /// 
                ///         // Actual code
                /// 
                ///         -> Decompiled
                ///         if( continueCodition )
                ///         {
                ///         }
                ///         else
                ///         {
                ///             // Actual code
                ///         }
                /// 
                /// 
                ///     2:(-> ...)  ...
                ///         -> Original
                ///             ...
                ///         -> Decompiled
                ///             ...
                /// 
                /// </summary>
                /// <param name="decompiler"></param>
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    // Remove 'else' marking as long as other fallback are better suited
                    var tempLinkedIf = LinkedIfNest?.HasElseNest;
                    if (tempLinkedIf != null)
                    {
                        LinkedIfNest.HasElseNest = null;
                    }

                    // Break offset!
                    if (CodeOffset >= Position)
                    {
                        if (MarkedAsSwitchBreak
                            //==================We're inside a Case and at the end of it!
                            || JumpsOutOfSwitch(decompiler) &&
                            decompiler.IsInNest(NestManager.Nest.NestType.Case) != null
                            //==================We're inside a Default and at the end of it!
                            || decompiler.IsInNest(NestManager.Nest.NestType.Default) != null)
                        {
                            NoJumpLabel(decompiler);
                            SetEndComment(decompiler);
                            // 'break' CodeOffset sits at the end of the switch,
                            // check that it doesn't exist already and add it
                            int switchEnd = decompiler.IsInNest(NestManager.Nest.NestType.Default) != null
                                ? Position + Size
                                : CodeOffset;
                            decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, switchEnd);
                            decompiler.MarkSemicolon();

                            return "break";
                        }

                        if (decompiler.IsWithinNest(NestManager.Nest.NestType.ForEach)?.Creator is IteratorToken
                            iteratorToken)
                        {
                            // Jumps to the end of the foreach ?
                            if (CodeOffset == iteratorToken.CodeOffset)
                            {
                                if (decompiler.PreviousToken is IteratorNextToken)
                                {
                                    NoJumpLabel(decompiler);
                                    return string.Empty;
                                }

                                NoJumpLabel(decompiler);
                                SetEndComment(decompiler);
                                decompiler.MarkSemicolon();

                                return "break";
                            }

                            if (decompiler.TokenAt(CodeOffset) is IteratorNextToken)
                            {
                                NoJumpLabel(decompiler);
                                SetEndComment(decompiler);
                                decompiler.MarkSemicolon();

                                return "continue";
                            }
                        }

                        if (decompiler.IsWithinNest(NestManager.Nest.NestType.Loop)?.Creator is JumpToken destJump)
                        {
                            if (CodeOffset + 10 == destJump.CodeOffset)
                            {
                                SetStatementComment(decompiler, "Explicit Continue");

                                goto gotoJump;
                            }

                            if (CodeOffset == destJump.CodeOffset)
                            {
                                SetStatementComment(decompiler, "Explicit Break");

                                goto gotoJump;
                            }
                        }

                        if (tempLinkedIf != null)
                        {
                            // Would this potential else scope break out of one of its parent scope
                            foreach (var nest in decompiler._Nester.Nests)
                            {
                                if (nest is NestManager.NestEnd outerNestEnd
                                    && CodeOffset > outerNestEnd.Position
                                    // It's not this if-else scope
                                    && LinkedIfNest.Creator != outerNestEnd.Creator)
                                {
                                    // this is more likely a continue within a for(;;) loop
                                    SetStatementComment(decompiler, "Explicit Continue");

                                    goto gotoJump;
                                }
                            }

                            // this is indeed the else part of an if-else, re-instate the link
                            // and let nest decompilation handle the rest
                            LinkedIfNest.HasElseNest = tempLinkedIf;
                            NoJumpLabel(decompiler);
                            decompiler.PopSemicolon();

                            return "";
                        }

                        // This can be inaccurate if the source goto jumps from within a case to in the middle of a default
                        // If that's the case the nest decompilation process should spew comments about it
                        if (JumpsOutOfSwitch(decompiler))
                        {
                            NoJumpLabel(decompiler);
                            SetEndComment(decompiler);
                            // 'break' CodeOffset sits at the end of the switch,
                            // check that it doesn't exist already and add it
                            decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, CodeOffset);

                            decompiler.MarkSemicolon();

                            return "break";
                        }
                    }

                    if (CodeOffset < Position)
                    {
                        SetStatementComment(decompiler, "Loop Continue");
                    }

                gotoJump:
                    if (Position + Size == CodeOffset)
                    {
                        // Remove jump to next token
                        NoJumpLabel(decompiler);

                        return "";
                    }

                    // This is an implicit GoToToken.
                    decompiler.MarkSemicolon();

                    return $"goto {UDecompilingState.OffsetLabelName(CodeOffset)}";
                }

                public bool JumpsOutOfSwitch(UByteCodeDecompiler decompiler)
                {
                    Token t;
                    for (int i = decompiler.DeserializedTokens.IndexOf(this) + 1;
                         i < decompiler.DeserializedTokens.Count &&
                         (t = decompiler.DeserializedTokens[i]).Position <= CodeOffset;
                         i++)
                    {
                        // Skip switch nests
                        if (t is SwitchToken)
                        {
                            var switchBalance = 1;
                            for (i += 1;
                                 i < decompiler.DeserializedTokens.Count && switchBalance > 0 &&
                                 (t = decompiler.DeserializedTokens[i]).Position <= CodeOffset;
                                 i++)
                            {
                                if (t is CaseToken ct && ct.IsDefault)
                                    switchBalance -= 1;
                                else if (t is SwitchToken)
                                    switchBalance += 1;
                            }
                        }
                        else if (t is CaseToken ct && ct.IsDefault && CodeOffset > ct.Position)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                private void NoJumpLabel(UByteCodeDecompiler decompiler)
                {
                    int i = decompiler._TempLabels.FindIndex(p => p.entry.Position == CodeOffset);
                    if (i == -1)
                    {
                        return;
                    }

                    var data = decompiler._TempLabels[i];
                    if (data.refs == 1)
                    {
                        decompiler._TempLabels.RemoveAt(i);
                    }
                    else
                    {
                        data.refs -= 1;
                        decompiler._TempLabels[i] = data;
                    }
                }
            }

            [ExprToken(ExprToken.JumpIfNot)]
            public class JumpIfNotToken : JumpToken
            {
                public bool IsLoop;

                public Token ConditionalExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    // CodeOffset
                    base.Deserialize(stream);

                    // Condition
                    ConditionalExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    base.Serialize(stream);

                    Script.SerializeToken(stream, ConditionalExpression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    string condition = DecompileNext(decompiler);

                    // Check if we are jumping to the start of a JumpIfNot token.
                    // if true, we can assume that this (If) statement is contained within a loop.
                    IsLoop = false;
                    for (int i = decompiler.CurrentTokenIndex + 1; i < decompiler.DeserializedTokens.Count; ++i)
                    {
                        if (decompiler.DeserializedTokens[i] is JumpToken jt && jt.CodeOffset == Position)
                        {
                            IsLoop = true;
                            break;
                        }
                    }

                    SetEndComment(decompiler);
                    if (IsLoop)
                    {
                        decompiler.PreComment += " [Loop If]";
                    }

                    string output;
                    if ((CodeOffset & ushort.MaxValue) < Position)
                    {
                        string labelName = UDecompilingState.OffsetLabelName(CodeOffset);
                        var gotoStatement = $"{UDecompilingState.Tabs}{UnrealConfig.Indention}goto {labelName}";
                        // Inverse condition only here as we're explicitly jumping while other cases create proper scopes 
                        output = $"if(!({condition}))\r\n{gotoStatement}";
                        decompiler.MarkSemicolon();
                        return output;
                    }

                    output = /*(IsLoop ? "while" : "if") +*/ $"if({condition})";
                    decompiler.PopSemicolon();

                    if (IsLoop == false)
                    {
                        int i;
                        for (i = decompiler.DeserializedTokens.IndexOf(this);
                             i < decompiler.DeserializedTokens.Count &&
                             (decompiler.DeserializedTokens[i]).Position < CodeOffset;
                             i++)
                        {
                            // Seek to jump destination
                        }

                        var prevToken = decompiler.DeserializedTokens[i - 1];
                        var elseStartToken = decompiler.DeserializedTokens[i];

                        // Test to see if this JumpIfNotToken is the if part of an if-else nest
                        if (elseStartToken.Position == CodeOffset && prevToken is JumpToken ifEndJump)
                        {
                            if (elseStartToken is CaseToken && ifEndJump.JumpsOutOfSwitch(decompiler))
                            {
                                // It's an if containing a break. When the if is *not* taken, execution continues inside of a case below
                                ifEndJump.MarkedAsSwitchBreak = true;
                            }
                            else if (elseStartToken.Position == CodeOffset &&
                                     ifEndJump.CodeOffset != elseStartToken.Position)
                            {
                                // Most likely an if-else, mark it as such and let the rest of the logic figure it out further
                                int begin = Position;
                                const NestManager.Nest.NestType type = NestManager.Nest.NestType.If;
                                decompiler._Nester.Nests.Add(new NestManager.NestBegin
                                { Position = begin, Type = type, Creator = this });
                                var nestEnd = new NestManager.NestEnd
                                {
                                    Position = CodeOffset,
                                    Type = type,
                                    Creator = this,
                                    HasElseNest = ifEndJump,
                                };
                                decompiler._Nester.Nests.Add(nestEnd);

                                var outdatedLink = ifEndJump.LinkedIfNest;
                                // This will hint to the jump token that it is likely an else scope
                                ifEndJump.LinkedIfNest = nestEnd;
                                // Let's make sure we break any previous link this jump had with other 'if's
                                // the most recent is the most accurate
                                if (outdatedLink != null)
                                    outdatedLink.HasElseNest = null;
                                return output;
                            }
                        }
                    }

                    decompiler._Nester.AddNest(IsLoop
                                                   ? NestManager.Nest.NestType.Loop
                                                   : NestManager.Nest.NestType.If,
                                               Position, CodeOffset, this
                    );
                    return output;
                }
            }

            [ExprToken(ExprToken.FilterEditorOnly)]
            public class FilterEditorOnlyToken : JumpToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler._Nester.AddNest(NestManager.Nest.NestType.Scope, Position, CodeOffset);
                    SetEndComment(decompiler);

                    return "filtereditoronly";
                }
            }

            [ExprToken(ExprToken.Switch)]
            public class SwitchToken : Token
            {
                public UField ExpressionField;
                public ushort PropertyType;

                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    // Skip Tera (610)
                    if (stream.Version >= 611)
                    {
                        // Points to the object that was passed to the switch,
                        // beware that the followed token chain contains it as well!
                        stream.Read(out ExpressionField);
                        Script.AlignObjectSize();
                    }

                    // FIXME: version
                    if (stream.Version is >= 536 and <= 587
#if DNF
                        || stream.Build == UnrealPackage.GameBuild.BuildName.DNF
#endif
#if TERA
                        || stream.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
                       )
                    {
                        PropertyType = stream.ReadUInt16();
                        Script.AlignSize(sizeof(ushort));
                    }
                    else
                    {
                        PropertyType = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                deserialize:
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    // Skip Tera (610)
                    if (stream.Version >= 611)
                    {
                        // Points to the object that was passed to the switch,
                        // beware that the followed token chain contains it as well!
                        stream.Write(ExpressionField);
                        Script.AlignObjectSize();
                    }

                    // FIXME: version
                    if (stream.Version is >= 536 and <= 587
#if DNF
                        || stream.Build == UnrealPackage.GameBuild.BuildName.DNF
#endif
#if TERA
                        || stream.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
                       )
                    {
                        stream.Write((ushort)PropertyType);
                        Script.AlignSize(sizeof(ushort));
                    }
                    else
                    {
                        stream.Write((byte)PropertyType);
                        Script.AlignSize(sizeof(byte));
                    }

                deserialize:
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                /// <summary>
                /// FORMATION ISSUESSES:
                ///     1:(-> ...)  NestEnd is based upon the break in a default case, however a case does not always break/return,
                ///         causing that there will be no default with a break detected, thus no ending for the Switch block.
                /// 
                ///         -> Original
                ///             Switch( A )
                ///             {
                ///                 case 0:
                ///                     CallA();
                ///             }
                /// 
                ///             CallB();
                /// 
                ///         -> Decompiled
                ///             Switch( A )
                ///             {
                ///                 case 0:
                ///                     CallA();
                ///                 default:    // End is detect of case 0 due some other hack :)
                ///                     CallB();
                /// </summary>
                /// <param name="decompiler"></param>
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler._Nester.AddNestBegin(NestManager.Nest.NestType.Switch, Position);

                    string expr = DecompileNext(decompiler);
                    decompiler.PopSemicolon();

                    return $"switch({expr})";
                }
            }

            [ExprToken(ExprToken.Case)]
            public class CaseToken : JumpToken
            {
                public Token? ConditionalExpression;

                public bool IsDefault => CodeOffset == ushort.MaxValue;

                public override void Deserialize(IUnrealStream stream)
                {
                    base.Deserialize(stream);

                    if (CodeOffset != ushort.MaxValue)
                    {
                        ConditionalExpression = Script.DeserializeNextToken(stream);
                    } // Else "Default:"
                }

                public override void Serialize(IUnrealStream stream)
                {
                    base.Serialize(stream);

                    if (CodeOffset != ushort.MaxValue)
                    {
                        Contract.Assert(ConditionalExpression != null);
                        Script.SerializeToken(stream, ConditionalExpression);
                    } // Else "Default:"
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    SetEndComment(decompiler);
                    if (CodeOffset != ushort.MaxValue)
                    {
                        decompiler._Nester.AddNest(NestManager.Nest.NestType.Case, Position, CodeOffset);
                        var output = $"case {DecompileNext(decompiler)}:";
                        decompiler.PopSemicolon();

                        return output;
                    }

                    decompiler._Nester.AddNestBegin(NestManager.Nest.NestType.Default, Position, this);
                    decompiler.PopSemicolon();

                    return "default:";
                }
            }

            [ExprToken(ExprToken.Iterator)]
            public class IteratorToken : JumpToken
            {
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Expression = Script.DeserializeNextToken(stream);

                    base.Deserialize(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    base.Serialize(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler._Nester.AddNest(NestManager.Nest.NestType.ForEach, Position, CodeOffset, this);
                    SetEndComment(decompiler);

                    // foreach FunctionCall
                    string expression = DecompileNext(decompiler);
                    decompiler.PopSemicolon(); // Undo

                    return $"foreach {expression}";
                }
            }

            [ExprToken(ExprToken.DynArrayIterator)]
            public class DynamicArrayIteratorToken : JumpToken
            {
                public Token Expression;
                public Token ItemArgument;
                public byte WithIndexParam;
                public Token IndexArgument;

                public override void Deserialize(IUnrealStream stream)
                {
                    Expression = Script.DeserializeNextToken(stream);
                    ItemArgument = Script.DeserializeNextToken(stream);

                    WithIndexParam = stream.ReadByte();
                    Script.AlignSize(sizeof(byte));

                    IndexArgument = Script.DeserializeNextToken(stream);

                    base.Deserialize(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                    Contract.Assert(ItemArgument != null);
                    Script.SerializeToken(stream, ItemArgument);

                    stream.Write(WithIndexParam);
                    Script.AlignSize(sizeof(byte));

                    Contract.Assert(IndexArgument != null);
                    Script.SerializeToken(stream, IndexArgument);

                    base.Serialize(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler._Nester.AddNest(NestManager.Nest.NestType.ForEach, Position, CodeOffset, this);

                    SetEndComment(decompiler);

                    // foreach ArrayVariable( Parameters )
                    string output;
                    if (WithIndexParam > 0)
                    {
                        output =
                            $"foreach {DecompileNext(decompiler)}({DecompileNext(decompiler)}, {DecompileNext(decompiler)})";
                    }
                    else
                    {
                        output = $"foreach {DecompileNext(decompiler)}({DecompileNext(decompiler)})";
                        // Skip Index param
                        NextToken(decompiler);
                    }

                    decompiler.PopSemicolon();

                    return output;
                }
            }

            [ExprToken(ExprToken.IteratorNext)]
            public class IteratorNextToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    if (decompiler.PeekToken is IteratorPopToken)
                    {
                        return string.Empty;
                    }

                    decompiler.MarkSemicolon();

                    return "continue";
                }
            }

            [ExprToken(ExprToken.IteratorPop)]
            public class IteratorPopToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    if (decompiler.PreviousToken is IteratorNextToken
                        || decompiler.PeekToken is ReturnToken)
                    {
                        return string.Empty;
                    }

                    decompiler.MarkSemicolon();

                    return "break";
                }
            }

            private List<ULabelEntry>? _Labels;
            private List<(ULabelEntry entry, int refs)> _TempLabels;

            [ExprToken(ExprToken.LabelTable)]
            public class LabelTableToken : Token
            {
                public List<ULabelEntry> Labels { get; set; }

                public override void Deserialize(IUnrealStream stream)
                {
                    Labels = [];

                    var label = UnrealName.None;
                    int labelPos = -1;
                    do
                    {
                        if (label != UnrealName.None)
                        {
                            Labels.Add(new ULabelEntry
                            {
                                Name = label,
                                Position = labelPos
                            });
                        }

                        label = stream.ReadName();
                        Script.AlignNameSize();
                        labelPos = stream.ReadInt32();
                        Script.AlignSize(sizeof(int));
                    } while (label != UnrealName.None);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Labels != null);

                    foreach (var label in Labels)
                    {
                        stream.WriteName(label.Name);
                        Script.AlignNameSize();
                        stream.Write(label.Position);
                        Script.AlignSize(sizeof(int));
                    }

                    stream.WriteName(UnrealName.None);
                    Script.AlignNameSize();
                    stream.Write(int.MaxValue);
                    Script.AlignSize(sizeof(int));
                }
            }

            [ExprToken(ExprToken.Skip)]
            public class SkipToken : Token
            {
                public ushort Size;
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Size = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));

                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Size);
                    Script.AlignSize(sizeof(ushort));

                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileNext(decompiler);
                }
            }

            [ExprToken(ExprToken.Stop)]
            public class StopToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return "stop";
                }
            }
        }
    }
}