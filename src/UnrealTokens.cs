namespace UELib.Tokens
{
    /// <summary>
    /// A collection of tokens describing an expression.
    /// </summary>
    public enum ExprToken : ushort
    {
        // ValidateObject
        LocalVariable           = 0x00,
        InstanceVariable        = 0x01,
        DefaultVariable         = 0x02,     // default.Property
        /// <summary>
        /// UE1: ???
        /// UE2: Deprecated (Bad Expr Token)
        /// UE3: Introduced in a late UDK build.
        /// </summary>
        StateVariable           = 0x03,
        Return                  = 0x04,     // return EXPRESSION
        Switch                  = 0x05,     // switch (CONDITION)
        Jump                    = 0x06,     // goto CODEOFFSET
        JumpIfNot               = 0x07,     // if( !CONDITION ) goto CODEOFFSET;
        Stop                    = 0x08,     // Stop         (State)
        Assert                  = 0x09,     // assert (CONDITION)
        Case                    = 0x0A,     // case CONDITION:
        Nothing                 = 0x0B,
        LabelTable              = 0x0C,
        GotoLabel               = 0x0D,     // goto EXPRESSION
        EatString               = 0x0E,
        EatReturnValue          = 0x0E,     // Formerly known as EatString
        Let                     = 0x0F,     // A = B
        DynArrayElement         = 0x10,     // Array[EXPRESSION]
        New                     = 0x11,     // new(OUTER) CLASS...
        ClassContext            = 0x12,     // Class'Path'.static.Function()
        MetaCast                = 0x13,     // <CLASS>(CLASS)

        /// <summary>
        /// UE1: BeginFunction
        /// UE2: LetBool
        /// </summary>
        BeginFunction           = 0x14,
        LetBool                 = 0x14,

        /// <summary>
        /// UE1: >68? LineNumber
        /// UE2: LineNumber (early UE2)?, else (Bad Expr Token)
        /// UE3: EndParmValue
        /// </summary>
        LineNumber              = 0x15,
        EndParmValue            = 0x15,
        EndFunctionParms        = 0x16,     // )
        Self                    = 0x17,     // Self
        Skip                    = 0x18,
        Context                 = 0x19,     // A.B
        ArrayElement            = 0x1A,     // A[x]
        VirtualFunction         = 0x1B,     // F(...)
        FinalFunction           = 0x1C,     // F(...)
        IntConst                = 0x1D,
        FloatConst              = 0x1E,
        StringConst             = 0x1F,     // "String"
        ObjectConst             = 0x20,
        NameConst               = 0x21,     // 'Name'
        RotationConst           = 0x22,
        VectorConst             = 0x23,
        ByteConst               = 0x24,
        IntZero                 = 0x25,
        IntOne                  = 0x26,
        True                    = 0x27,
        False                   = 0x28,
        NativeParm              = 0x29,     // A            (Native)
        NoObject                = 0x2A,     // None
        /// <summary>
        /// UE1: A string size cast
        /// UE2: Deprecated (Bad Expr Token)
        /// </summary>
        CastStringSize          = 0x2B,
        IntConstByte            = 0x2C,     // 0-9          (<= 255)
        BoolVariable            = 0x2D,     // B            (Bool)
        DynamicCast             = 0x2E,     // A(B)
        Iterator                = 0x2F,     // ForEach
        IteratorPop             = 0x30,     // Break        (Implied/Explicit)
        IteratorNext            = 0x31,     // Continue     (Implied/Explicit)
        StructCmpEq             = 0x32,     // A == B
        StructCmpNE             = 0x33,     // A != B
        // UnicodeStringConst
        UniStringConst          = 0x34,     // "UNICODE"

        // Note: These byte-codes have shifted since UE3 and have therefor incorrect values assigned.
        #region FixedByteCodes
        /// <summary>
        /// UE1: ???
        /// UE2: RangeConst or Deprecated (Bad Expr Token)
        /// UE3: ???
        /// </summary>
        RangeConst              = 0x35,
        StructMember            = 0x36,     // Struct.Property
        DynArrayLength          = 0x37,     // ARRAY.Length
        GlobalFunction          = 0x38,     // Global.

        /// <summary>
        /// Redefined(RotatorToVector)
        ///
        /// UE1: RotatorToVector cast.
        /// UE2+: Followed by any of the CastTokens to free space for other tokens, most are unused from 0x39 to 0x3F.
        /// </summary>
        PrimitiveCast           = 0x39,     // TYPE(EXPRESSION)
        #endregion
        
        /// <summary>
        /// Redefined(DynArrayRemove)
        ///
        /// UE1: ByteToInt cast.
        /// UE2: ReturnNothing (Deprecated)
        /// UE3: ReturnNothing if previous token is a ReturnToken, DynArrayRemove when not.
        /// </summary>
        ReturnNothing           = 0x3A,

        // UE2:ReturnNothing (Deprecated)
        DelegateCmpEq           = 0x3B,
        DelegateCmpNE           = 0x3C,
        DelegateFunctionCmpEq   = 0x3D,
        DelegateFunctionCmpNE   = 0x3E,
        NoDelegate              = 0x3F,

        // Note: These byte-codes have shifted since UE3 and have therefor incorrect values assigned.
        #region FixedByteCodes
        DynArrayInsert          = 0x40,
        DynArrayRemove          = 0x41,
        DebugInfo               = 0x42,
        DelegateFunction        = 0x43,
        DelegateProperty        = 0x44,
        LetDelegate             = 0x45,
        /// <summary>
        /// UE3: An alternative to Else-If statements using A ? B : C;
        /// </summary>
        Conditional             = 0x46,     // CONDITION ? TRUE_LET : FALSE_LET
        /// <summary>
        /// Redefined(ObjectToBool,DynArrayFind)
        ///
        /// UE1: As an ObjectToBool cast.
        /// UE2: As an indicator of a function's end(unless preceded by PrimitiveCast then it is treat as an ObjectToBool).
        /// UE3: See DynArrayFind(See EndOfScript).
        /// </summary>
        FunctionEnd             = 0x47,
        /// <summary>
        /// Find an item within an Array.
        /// </summary>
        DynArrayFind            = 0x47,     // ARRAY.Find( EXPRESSION )
        /// <summary>
        /// UE3: Find an item within a struct in an Array.
        /// </summary>
        DynArrayFindStruct      = 0x48,     // ARRAY.Find( EXPRESSION, EXPRESSION )
        /// <summary>
        /// In some Unreal Engine 2 games, see Conditional for Unreal Engine 3.
        ///
        /// An alternative to Else-If statements using A ? B : C;.
        /// </summary>
        Eval                    = 0x48,     // See Conditional
        /// <summary>
        /// UE3: Reference to a property with the Out modifier.
        /// </summary>
        OutVariable             = 0x49,
        /// <summary>
        /// UE3: Default value of a parameter property.
        /// </summary>
        DefaultParmValue        = 0x4A,     // PARAMETER = EXPRESSION
        /// <summary>
        /// UE3: No parameter value was given e.g: Foo( Foo,, Foo );
        /// </summary>
        EmptyParmValue          = 0x4B,     // Empty argument, Call(Parm1,,Parm2)
        InstanceDelegate        = 0x4C,
        VarInt                  = 0x4D,     // Found in Borderlands 2
        VarFloat                = 0x4E,     // Found in Borderlands 2
        VarByte                 = 0x4F,     // Found in Borderlands 2
        VarBool                 = 0x50,     // Found in Borderlands 2
        VarObject               = 0x51,     // Found in Borderlands 2
        StringRef               = 0x50,     // Found in Mirrors Edge
        UndefinedVariable       = 0x51,     // Found in Gears of War
        InterfaceContext        = 0x52,
        InterfaceCast           = 0x53,
        EndOfScript             = 0x54,
        DynArrayAdd             = 0x55,
        DynArrayAddItem         = 0x56,
        DynArrayRemoveItem      = 0x57,
        DynArrayInsertItem      = 0x58,
        DynArrayIterator        = 0x59,
        DynArraySort            = 0x5A,
        FilterEditorOnly        = 0x5B,     // filtereditoronly { BLOCK }
        Unused5C                = 0x5C,
        Unused5D                = 0x5D,
        Unused5E                = 0x5E,
        Unused5F                = 0x5F,
        #endregion

        ExtendedNative          = 0x60,
        FirstNative             = 0x70,
        MaxNative               = 0x1000,
        
        InternalUnresolved      = 0xFF,
        Unused                  = InternalUnresolved,
    }

    /// <summary>
    /// These tokens begin at-non-zero because they were once part of <see cref="ExprToken"/> &lt; <see cref="UStruct.PrimitiveCastVersion"/>.
    /// Handled by token: <see cref="UStruct.UByteCodeDecompiler.PrimitiveCastToken"/>
    /// </summary>
    public enum CastToken : byte
    {
        None                    = 0x00,
        
        #region UE3
        InterfaceToObject       = 0x36,
        InterfaceToString       = 0x37,
        InterfaceToBool         = 0x38,
        #endregion

        RotatorToVector         = 0x39,
        ByteToInt               = 0x3A,
        ByteToBool              = 0x3B,
        ByteToFloat             = 0x3C,
        IntToByte               = 0x3D,
        IntToBool               = 0x3E,
        IntToFloat              = 0x3F,
        BoolToByte              = 0x40,
        BoolToInt               = 0x41,
        BoolToFloat             = 0x42,
        FloatToByte             = 0x43,
        FloatToInt              = 0x44,
        FloatToBool             = 0x45,
        /// <summary>
        /// UE1: StringToName
        /// UE2: Deprecated?
        /// </summary>
        ObjectToInterface       = 0x46,
        ObjectToBool            = 0x47,
        NameToBool              = 0x48,
        StringToByte            = 0x49,
        StringToInt             = 0x4A,
        StringToBool            = 0x4B,
        StringToFloat           = 0x4C,
        StringToVector          = 0x4D,
        StringToRotator         = 0x4E,
        VectorToBool            = 0x4F,
        VectorToRotator         = 0x50,
        RotatorToBool           = 0x51,
        ByteToString            = 0x52,
        IntToString             = 0x53,
        BoolToString            = 0x54,
        FloatToString           = 0x55,
        ObjectToString          = 0x56,
        NameToString            = 0x57,
        VectorToString          = 0x58,
        RotatorToString         = 0x59,

        #region UE3
        DelegateToString        = 0x5A,
        StringToName            = 0x60,
        #endregion
    }

    public enum DebugInfo
    {
        Let                 = 0x00,
        SimpleIf            = 0x01,
        Switch              = 0x02,
        While               = 0x03,
        Assert              = 0x04,
        Return              = 0x10,
        ReturnNothing       = 0x11,
        NewStack            = 0x20,
        NewStackLatent      = 0x21,
        NewStackLabel       = 0x22,
        PrevStack           = 0x30,
        PrevStackLatent     = 0x31,
        PrevStackLabel      = 0x32,
        PrevStackState      = 0x33,
        EFP                 = 0x40,
        EFPOper             = 0x41,
        EFPIter             = 0x42,
        ForInit             = 0x50,
        ForEval             = 0x51,
        ForInc              = 0x52,
        BreakLoop           = 0x60,
        BreakFor            = 0x61,
        BreakForEach        = 0x62,
        BreakSwitch         = 0x63,
        ContinueLoop        = 0x70,
        ContinueForeach     = 0x71,
        ContinueFor         = 0x72,
        
        Unset               = 0xFF,
        
        // Older names, we need these to help with the conversion of a string to OpCode.
        OperEFP             = EFPOper,
        IterEFP             = EFPIter,
    }
}