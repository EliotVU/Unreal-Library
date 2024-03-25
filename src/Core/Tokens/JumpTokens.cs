using System;
using System.Collections.Generic;
using UELib.Branch;
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
                public override void Deserialize(IUnrealStream stream)
                {
                    if (Decompiler._Buffer.Version < (uint)PackageObjectLegacyVersion.ReturnExpressionAddedToReturnToken)
                    {
                        return;
                    }

                    // Expression
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    // HACK: for case's that end with a return instead of a break.
                    if (Decompiler.IsInNest(NestManager.Nest.NestType.Default) != null)
                    {
                        Decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, Position + Size);
                    }

                    Decompiler.MarkSemicolon();
                    if (Decompiler._Buffer.Version < (uint)PackageObjectLegacyVersion.ReturnExpressionAddedToReturnToken)
                    {
                        // FIXME: Transport the emitted "ReturnValue = Expression" over here.
                        return "return";
                    }

                    string returnValue = DecompileNext();
                    return "return" + (returnValue.Length != 0 
                        ? " " + returnValue 
                        : string.Empty);
                }
            }

            [ExprToken(ExprToken.ReturnNothing)]
            public class ReturnNothingToken : EatReturnValueToken
            {
                public override string Decompile()
                {
                    // HACK: for case's that end with a return instead of a break.
                    if (Decompiler.IsInNest(NestManager.Nest.NestType.Default) != null)
                    {
                        Decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, Position + Size);
                    }

                    return ReturnValueProperty != null 
                        ? ReturnValueProperty.Name 
                        : string.Empty;
                }
            }

            [ExprToken(ExprToken.GotoLabel)]
            public class GotoLabelToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    // Expression
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    Decompiler.MarkSemicolon();
                    return $"goto {DecompileNext()}";
                }
            }

            [ExprToken(ExprToken.Jump)]
            public class JumpToken : Token
            {
                public bool MarkedAsSwitchBreak;
                public NestManager.NestEnd LinkedIfNest;
                public ushort CodeOffset { get; private set; }

                public override void Deserialize(IUnrealStream stream)
                {
#if UE4
                    if (stream.UE4Version > 0)
                    {
                        CodeOffset = (ushort)stream.ReadInt32();
                        Decompiler.AlignSize(sizeof(int));
                        return;
                    }
#endif
                    CodeOffset = stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));
                }

                public override void PostDeserialized()
                {
                    if (GetType() == typeof(JumpToken))
                        Decompiler._Labels.Add
                        (
                            new ULabelEntry
                            {
                                Name = UDecompilingState.OffsetLabelName(CodeOffset),
                                Position = CodeOffset
                            }
                        );
                }

                protected void SetEndComment()
                {
                    Decompiler.PreComment = $"// End:0x{CodeOffset:X2}";
                }

                private void SetStatementComment(string statement)
                {
                    Decompiler.PreComment = $"// [{statement}]";
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
                public override string Decompile()
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
                            || JumpsOutOfSwitch() && Decompiler.IsInNest(NestManager.Nest.NestType.Case) != null
                            //==================We're inside a Default and at the end of it!
                            || Decompiler.IsInNest(NestManager.Nest.NestType.Default) != null)
                        {
                            NoJumpLabel();
                            SetEndComment();
                            // 'break' CodeOffset sits at the end of the switch,
                            // check that it doesn't exist already and add it
                            int switchEnd = Decompiler.IsInNest(NestManager.Nest.NestType.Default) != null
                                ? Position + Size
                                : CodeOffset;
                            Decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, switchEnd);
                            Decompiler._CanAddSemicolon = true;
                            return "break";
                        }

                        if (Decompiler.IsWithinNest(NestManager.Nest.NestType.ForEach)?.Creator is IteratorToken
                            iteratorToken)
                        {
                            // Jumps to the end of the foreach ?
                            if (CodeOffset == iteratorToken.CodeOffset)
                            {
                                if (Decompiler.PreviousToken is IteratorNextToken)
                                {
                                    NoJumpLabel();
                                    return string.Empty;
                                }

                                NoJumpLabel();
                                SetEndComment();
                                Decompiler._CanAddSemicolon = true;
                                return "break";
                            }

                            if (Decompiler.TokenAt(CodeOffset) is IteratorNextToken)
                            {
                                NoJumpLabel();
                                SetEndComment();
                                Decompiler._CanAddSemicolon = true;
                                return "continue";
                            }
                        }

                        if (Decompiler.IsWithinNest(NestManager.Nest.NestType.Loop)?.Creator is JumpToken destJump)
                        {
                            if (CodeOffset + 10 == destJump.CodeOffset)
                            {
                                SetStatementComment("Explicit Continue");
                                goto gotoJump;
                            }

                            if (CodeOffset == destJump.CodeOffset)
                            {
                                SetStatementComment("Explicit Break");
                                goto gotoJump;
                            }
                        }

                        if (tempLinkedIf != null)
                        {
                            // Would this potential else scope break out of one of its parent scope
                            foreach (var nest in Decompiler._Nester.Nests)
                            {
                                if (nest is NestManager.NestEnd outerNestEnd
                                    && CodeOffset > outerNestEnd.Position
                                    // It's not this if-else scope
                                    && LinkedIfNest.Creator != outerNestEnd.Creator)
                                {
                                    // this is more likely a continue within a for(;;) loop
                                    SetStatementComment("Explicit Continue");
                                    goto gotoJump;
                                }
                            }

                            // this is indeed the else part of an if-else, re-instate the link
                            // and let nest decompilation handle the rest
                            LinkedIfNest.HasElseNest = tempLinkedIf;
                            NoJumpLabel();
                            Decompiler._CanAddSemicolon = false;
                            return "";
                        }

                        // This can be inaccurate if the source goto jumps from within a case to in the middle of a default
                        // If that's the case the nest decompilation process should spew comments about it
                        if (JumpsOutOfSwitch())
                        {
                            NoJumpLabel();
                            SetEndComment();
                            // 'break' CodeOffset sits at the end of the switch,
                            // check that it doesn't exist already and add it
                            Decompiler._Nester.TryAddNestEnd(NestManager.Nest.NestType.Switch, CodeOffset);

                            Decompiler._CanAddSemicolon = true;
                            return "break";
                        }
                    }

                    if (CodeOffset < Position)
                    {
                        SetStatementComment("Loop Continue");
                    }

                gotoJump:
                    if (Position + Size == CodeOffset)
                    {
                        // Remove jump to next token
                        NoJumpLabel();
                        return "";
                    }

                    // This is an implicit GoToToken.
                    Decompiler._CanAddSemicolon = true;
                    return $"goto {UDecompilingState.OffsetLabelName(CodeOffset)}";
                }

                public bool JumpsOutOfSwitch()
                {
                    Token t;
                    for (int i = Decompiler.DeserializedTokens.IndexOf(this) + 1;
                         i < Decompiler.DeserializedTokens.Count &&
                         (t = Decompiler.DeserializedTokens[i]).Position <= CodeOffset;
                         i++)
                    {
                        // Skip switch nests
                        if (t is SwitchToken)
                        {
                            var switchBalance = 1;
                            for (i += 1;
                                 i < Decompiler.DeserializedTokens.Count && switchBalance > 0 &&
                                 (t = Decompiler.DeserializedTokens[i]).Position <= CodeOffset;
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

                private void NoJumpLabel()
                {
                    int i = Decompiler._TempLabels.FindIndex(p => p.entry.Position == CodeOffset);
                    if (i != -1)
                    {
                        var data = Decompiler._TempLabels[i];
                        if (data.refs == 1)
                        {
                            Decompiler._TempLabels.RemoveAt(i);
                        }
                        else
                        {
                            data.refs -= 1;
                            Decompiler._TempLabels[i] = data;
                        }
                    }
                }
            }

            [ExprToken(ExprToken.JumpIfNot)]
            public class JumpIfNotToken : JumpToken
            {
                public bool IsLoop;

                public override void PostDeserialized()
                {
                    base.PostDeserialized();
                    // Add jump label for 'do until' jump pattern
                    if ((CodeOffset & ushort.MaxValue) < Position)
                    {
                        Decompiler._Labels.Add
                        (
                            new ULabelEntry
                            {
                                Name = UDecompilingState.OffsetLabelName(CodeOffset),
                                Position = CodeOffset
                            }
                        );
                    }
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    // CodeOffset
                    base.Deserialize(stream);

                    // Condition
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    string condition = DecompileNext();

                    // Check if we are jumping to the start of a JumpIfNot token.
                    // if true, we can assume that this (If) statement is contained within a loop.
                    IsLoop = false;
                    for (int i = Decompiler.CurrentTokenIndex + 1; i < Decompiler.DeserializedTokens.Count; ++i)
                    {
                        if (Decompiler.DeserializedTokens[i] is JumpToken jt && jt.CodeOffset == Position)
                        {
                            IsLoop = true;
                            break;
                        }
                    }

                    SetEndComment();
                    if (IsLoop)
                    {
                        Decompiler.PreComment += " [Loop If]";
                    }

                    string output;
                    if ((CodeOffset & ushort.MaxValue) < Position)
                    {
                        string labelName = UDecompilingState.OffsetLabelName(CodeOffset);
                        var gotoStatement = $"{UDecompilingState.Tabs}{UnrealConfig.Indention}goto {labelName}";
                        // Inverse condition only here as we're explicitly jumping while other cases create proper scopes 
                        output = $"if(!({condition}))\r\n{gotoStatement}";
                        Decompiler._CanAddSemicolon = true;
                        return output;
                    }

                    output = /*(IsLoop ? "while" : "if") +*/ $"if({condition})";
                    Decompiler._CanAddSemicolon = false;

                    if (IsLoop == false)
                    {
                        int i;
                        for (i = Decompiler.DeserializedTokens.IndexOf(this);
                             i < Decompiler.DeserializedTokens.Count &&
                             (Decompiler.DeserializedTokens[i]).Position < CodeOffset;
                             i++)
                        {
                            // Seek to jump destination
                        }

                        var prevToken = Decompiler.DeserializedTokens[i - 1];
                        var elseStartToken = Decompiler.DeserializedTokens[i];

                        // Test to see if this JumpIfNotToken is the if part of an if-else nest
                        if (elseStartToken.Position == CodeOffset && prevToken is JumpToken ifEndJump)
                        {
                            if (elseStartToken is CaseToken && ifEndJump.JumpsOutOfSwitch())
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
                                Decompiler._Nester.Nests.Add(new NestManager.NestBegin
                                    { Position = begin, Type = type, Creator = this });
                                var nestEnd = new NestManager.NestEnd
                                {
                                    Position = CodeOffset,
                                    Type = type,
                                    Creator = this,
                                    HasElseNest = ifEndJump,
                                };
                                Decompiler._Nester.Nests.Add(nestEnd);

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

                    Decompiler._Nester.AddNest(IsLoop
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
                public override string Decompile()
                {
                    Decompiler._Nester.AddNest(NestManager.Nest.NestType.Scope, Position, CodeOffset);
                    SetEndComment();
                    return "filtereditoronly";
                }
            }

            [ExprToken(ExprToken.Switch)]
            public class SwitchToken : Token
            {
                public UField ExpressionField;
                public ushort PropertyType;

                public override void Deserialize(IUnrealStream stream)
                {
                    if (stream.Version >= 600)
                    {
                        // Points to the object that was passed to the switch,
                        // beware that the followed token chain contains it as well!
                        stream.Read(out ExpressionField);
                        Decompiler.AlignObjectSize();
                    }

                    // FIXME: version
                    if ((stream.Version >= 536 && stream.Version <= 587)
#if DNF
                        || stream.Package.Build == UnrealPackage.GameBuild.BuildName.DNF
#endif
                        )
                    {
                        PropertyType = stream.ReadUInt16();
                        Decompiler.AlignSize(sizeof(ushort));
                    }
                    else
                    {
                        PropertyType = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                deserialize:
                    // Expression
                    DeserializeNext();
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
                public override string Decompile()
                {
                    Decompiler._Nester.AddNestBegin(NestManager.Nest.NestType.Switch, Position);

                    string expr = DecompileNext();
                    Decompiler._CanAddSemicolon = false; // In case the decompiled token was a function call
                    return $"switch({expr})";
                }
            }

            [ExprToken(ExprToken.Case)]
            public class CaseToken : JumpToken
            {
                public bool IsDefault => CodeOffset == ushort.MaxValue;

                public override void Deserialize(IUnrealStream stream)
                {
                    base.Deserialize(stream);
                    if (CodeOffset != ushort.MaxValue)
                    {
                        DeserializeNext(); // Condition
                    } // Else "Default:"
                }

                public override string Decompile()
                {
                    SetEndComment();
                    if (CodeOffset != ushort.MaxValue)
                    {
                        Decompiler._Nester.AddNest(NestManager.Nest.NestType.Case, Position, CodeOffset);
                        var output = $"case {DecompileNext()}:";
                        Decompiler._CanAddSemicolon = false;
                        return output;
                    }

                    Decompiler._Nester.AddNestBegin(NestManager.Nest.NestType.Default, Position, this);
                    Decompiler._CanAddSemicolon = false;
                    return "default:";
                }
            }

            [ExprToken(ExprToken.Iterator)]
            public class IteratorToken : JumpToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeNext(); // Expression
                    base.Deserialize(stream);
                }

                public override string Decompile()
                {
                    Decompiler._Nester.AddNest(NestManager.Nest.NestType.ForEach, Position, CodeOffset, this);
                    SetEndComment();

                    // foreach FunctionCall
                    string expression = DecompileNext();
                    Decompiler._CanAddSemicolon = false; // Undo
                    return $"foreach {expression}";
                }
            }

            [ExprToken(ExprToken.DynArrayIterator)]
            public class DynamicArrayIteratorToken : JumpToken
            {
                public byte WithIndexParam;

                public override void Deserialize(IUnrealStream stream)
                {
                    // Expression
                    DeserializeNext();

                    // Item param
                    DeserializeNext();

                    WithIndexParam = stream.ReadByte();
                    Decompiler.AlignSize(sizeof(byte));
                    
                    // Index param
                    DeserializeNext();

                    base.Deserialize(stream);
                }

                public override string Decompile()
                {
                    Decompiler._Nester.AddNest(NestManager.Nest.NestType.ForEach, Position, CodeOffset, this);

                    SetEndComment();

                    // foreach ArrayVariable( Parameters )
                    string output;
                    if (WithIndexParam > 0)
                    {
                        output = $"foreach {DecompileNext()}({DecompileNext()}, {DecompileNext()})";
                    }
                    else
                    {
                        output = $"foreach {DecompileNext()}({DecompileNext()})";
                        // Skip Index param
                        NextToken();
                    }

                    Decompiler._CanAddSemicolon = false;
                    return output;
                }
            }

            [ExprToken(ExprToken.IteratorNext)]
            public class IteratorNextToken : Token
            {
                public override string Decompile()
                {
                    if (Decompiler.PeekToken is IteratorPopToken)
                    {
                        return string.Empty;
                    }

                    Decompiler._CanAddSemicolon = true;
                    return "continue";
                }
            }

            [ExprToken(ExprToken.IteratorPop)]
            public class IteratorPopToken : Token
            {
                public override string Decompile()
                {
                    if (Decompiler.PreviousToken is IteratorNextToken
                        || Decompiler.PeekToken is ReturnToken)
                    {
                        return string.Empty;
                    }

                    Decompiler._CanAddSemicolon = true;
                    return "break";
                }
            }

            private List<ULabelEntry> _Labels;
            private List<(ULabelEntry entry, int refs)> _TempLabels;

            [ExprToken(ExprToken.LabelTable)]
            public class LabelTableToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    var label = string.Empty;
                    int labelPos = -1;
                    do
                    {
                        if (label != string.Empty)
                        {
                            Decompiler._Labels.Add
                            (
                                new ULabelEntry
                                {
                                    Name = label,
                                    Position = labelPos
                                }
                            );
                        }

                        label = stream.ReadName();
                        Decompiler.AlignNameSize();
                        labelPos = stream.ReadInt32();
                        Decompiler.AlignSize(sizeof(int));
                    } while (string.Compare(label, "None", StringComparison.OrdinalIgnoreCase) != 0);
                }
            }

            [ExprToken(ExprToken.Skip)]
            public class SkipToken : Token
            {
                public ushort Size;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    Size = stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));

                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext();
                }
            }

            [ExprToken(ExprToken.Stop)]
            public class StopToken : Token
            {
                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return "stop";
                }
            }
        }
    }
}
