using System;
using System.Collections.Generic;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal struct with the functionality to contain Constants, Enums, Structs and Properties.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UStruct : UField
    {
        // FIXME: Version, set 95 (Deus Ex: IW)
        private const int VPrimitiveCastToken = 95;

        private const int VCppText = 120;

        // FIXME: Version
        private const int VProcessedText = 129;

        // FIXME: Version
        private const int VFriendlyNameMoved = 160;

        /// <summary>
        /// Used to determine if UClass has an interfaces UArray, and the ObjectToInterface CastToken (among others). 
        /// FIXME: Version
        /// </summary>
        public const int VInterfaceClass = 220;

        private const int VStorageScriptSize = 639;

        #region Serialized Members

        [CanBeNull] public UTextBuffer ScriptText { get; private set; }
        [CanBeNull] public UTextBuffer ProcessedText { get; private set; }
        [CanBeNull] public UTextBuffer CppText { get; private set; }
        public UName FriendlyName { get; protected set; }

        public int Line;
        public int TextPos;

        protected uint StructFlags { get; set; }
        [CanBeNull] protected UField Children { get; private set; }
        protected int DataScriptSize { get; private set; }
        private int ByteScriptSize { get; set; }

        #endregion

        #region Script Members

        public IList<UConst> Constants { get; private set; }

        public IList<UEnum> Enums { get; private set; }

        public IList<UStruct> Structs { get; private set; }

        public List<UProperty> Variables { get; private set; }

        public List<UProperty> Locals { get; private set; }

        #endregion

        #region General Members

        /// <summary>
        /// Default Properties buffer offset
        /// </summary>
        protected long _DefaultPropertiesOffset;

        //protected uint _CodePosition;

        public long ScriptOffset { get; private set; }

        public UByteCodeDecompiler ByteCodeManager;

        #endregion

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();

            // --SuperField
            if (!Package.IsConsoleCooked() && _Buffer.UE4Version < 117)
            {
                ScriptText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(ScriptText), ScriptText);
            }

            Children = _Buffer.ReadObject<UField>();
            Record(nameof(Children), Children);

            // Moved to UFunction in UE3
            if (Package.Version < VFriendlyNameMoved)
            {
                FriendlyName = _Buffer.ReadNameReference();
                Record(nameof(FriendlyName), FriendlyName);
            }
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (_Buffer.LicenseeVersion >= 17)
                {
                    // Back-ported CppText
                    CppText = _Buffer.ReadObject<UTextBuffer>();
                    Record(nameof(CppText), CppText);
                    
                    var dnfTextObj2 = _Buffer.ReadObject();
                    Record(nameof(dnfTextObj2), dnfTextObj2);
                    
                    _Buffer.ReadArray(out UArray<UObject> dnfIncludeTexts);
                    Record(nameof(dnfIncludeTexts), dnfIncludeTexts);
                }

                if (_Buffer.LicenseeVersion >= 2)
                {
                    // Bool?
                    byte dnfByte = _Buffer.ReadByte();
                    Record(nameof(dnfByte), dnfByte);
                    
                    var dnfName = _Buffer.ReadNameReference();
                    Record(nameof(dnfName), dnfName);
                }
                
                goto lineData;
            }
#endif
            // Standard, but UT2004' derived games do not include this despite reporting version 128+
            if (Package.Version >= VCppText && _Buffer.UE4Version < 117
                && !Package.IsConsoleCooked() &&
                Package.Build != BuildGeneration.UE2_5)
            {
                CppText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(CppText), CppText);
            }
#if VENGEANCE
            // Introduced with BioShock
            if (Package.Build == BuildGeneration.Vengeance &&
                _Buffer.LicenseeVersion >= 29)
            {
                int vengeanceUnknownObject = _Buffer.ReadObjectIndex();
                Record(nameof(vengeanceUnknownObject), vengeanceUnknownObject);
            }
#endif
            // UE3 or UE2.5 build, it appears that StructFlags may have been merged from an early UE3 build.
            // UT2004 reports version 26, and BioShock version 2
            if ((Package.Build == BuildGeneration.UE2_5 && _Buffer.LicenseeVersion >= 26) ||
                (Package.Build == BuildGeneration.Vengeance && _Buffer.LicenseeVersion >= 2))
            {
                StructFlags = _Buffer.ReadUInt32();
                Record(nameof(StructFlags), (StructFlags)StructFlags);
            }
#if VENGEANCE
            if (Package.Build == BuildGeneration.Vengeance &&
                _Buffer.LicenseeVersion >= 14)
            {
                ProcessedText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(ProcessedText), ProcessedText);
            }
#endif
            lineData:
            if (!Package.IsConsoleCooked() &&
                _Buffer.UE4Version < 117)
            {
                Line = _Buffer.ReadInt32();
                Record(nameof(Line), Line);
                TextPos = _Buffer.ReadInt32();
                Record(nameof(TextPos), TextPos);
                //var MinAlignment = _Buffer.ReadInt32();
                //Record(nameof(MinAlignment), MinAlignment);
            }
#if UNREAL2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                // Always zero in all of the Core.u structs
                int unknownInt32 = _Buffer.ReadInt32();
                Record("Unknown:Unreal2", unknownInt32);
            }
#endif
#if TRANSFORMERS
            if (Package.Build == BuildGeneration.HMS)
            {
                int transformersEndLine = _Buffer.ReadInt32();
                // The line where the struct's code body ends.
                Record(nameof(transformersEndLine), transformersEndLine);
            }
#endif
            ByteScriptSize = _Buffer.ReadInt32();
            Record(nameof(ByteScriptSize), ByteScriptSize);
            bool hasFixedScriptSize = Package.Version >= VStorageScriptSize;
            if (hasFixedScriptSize)
            {
                DataScriptSize = _Buffer.ReadInt32();
                Record(nameof(DataScriptSize), DataScriptSize);
            }
            else
            {
                DataScriptSize = ByteScriptSize;
            }

            ScriptOffset = _Buffer.Position;

            // Code Statements
            if (DataScriptSize <= 0)
                return;

            ByteCodeManager = new UByteCodeDecompiler(this);
            if (hasFixedScriptSize)
            {
                _Buffer.Skip(DataScriptSize);
            }
            else
            {
                const int moonbaseVersion = 587;
                const int shadowcomplexVersion = 590;

                bool isTrueScriptSize = Package.Build == UnrealPackage.GameBuild.BuildName.MOHA ||
                                        (
                                            Package.Version >= UnrealPackage.VINDEXDEPRECATED
                                            && (Package.Version < moonbaseVersion &&
                                                Package.Version > shadowcomplexVersion)
                                        );
                if (isTrueScriptSize)
                {
                    _Buffer.Skip(DataScriptSize);
                }
                else
                {
                    ByteCodeManager.Deserialize();
                }
            }
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                //_Buffer.ReadByte();
            }
#endif
        }

        protected override bool CanDisposeBuffer()
        {
            return base.CanDisposeBuffer() && ByteCodeManager == null;
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            if (Children == null)
                return;

            try
            {
                FindChildren();
            }
            catch (InvalidCastException ice)
            {
                Console.WriteLine(ice.Message);
            }
        }

        [Obsolete("Pending deprecation")]
        protected virtual void FindChildren()
        {
            Constants = new List<UConst>();
            Enums = new List<UEnum>();
            Structs = new List<UStruct>();
            Variables = new List<UProperty>();

            for (var child = Children; child != null; child = child.NextField)
            {
                if (child.GetType().IsSubclassOf(typeof(UProperty)))
                {
                    Variables.Add((UProperty)child);
                }
                else if (child.IsClassType("Const"))
                {
                    Constants.Insert(0, (UConst)child);
                }
                else if (child.IsClassType("Enum"))
                {
                    Enums.Insert(0, (UEnum)child);
                }
                else if (child is UStruct && ((UStruct)(child)).IsPureStruct())
                {
                    Structs.Insert(0, (UStruct)child);
                }
            }

            // TODO: Introduced since UDK 2011-06+(not sure on exaclty which month).
            if ((Package.Version >= 805 && GetType() == typeof(UState)) || GetType() == typeof(UFunction))
            {
                Locals = new List<UProperty>();
                foreach (var local in Variables)
                {
                    if (!local.IsParm())
                    {
                        Locals.Add(local);
                    }
                }
            }
        }

        #endregion

        #region Methods

        public bool HasStructFlag(StructFlags flag)
        {
            return (StructFlags & (uint)flag) != 0;
        }

        public bool IsPureStruct()
        {
            return IsClassType("Struct") || IsClassType("ScriptStruct");
        }

        #endregion
    }
}