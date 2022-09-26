namespace UELib.Tokens
{
    /// <summary>
    /// A collection of tokens describing an expression.
    /// </summary>
    public enum ExprToken : ushort
    {
        // ValidateObject
        LocalVariable,
        InstanceVariable,
        DefaultVariable, // default.Property
        StateVariable,
        Return, // return EXPRESSION
        Switch, // switch (CONDITION)
        Jump, // goto CODEOFFSET
        JumpIfNot, // if( !CONDITION ) goto CODEOFFSET;
        Stop, // Stop         (State)
        Assert, // assert (CONDITION)
        Case, // case CONDITION:
        Nothing,
        LabelTable,
        GotoLabel, // goto EXPRESSION
        EatString,
        EatReturnValue, // Formerly known as EatString
        Let, // A = B
        DynArrayElement, // Array[EXPRESSION]
        New, // new(OUTER) CLASS...
        ClassContext, // Class'Path'.static.Function()
        MetaCast, // <CLASS>(CLASS)
        BeginFunction,
        LetBool,
        LineNumber,
        EndParmValue,
        EndFunctionParms, // )
        Self, // Self
        Skip,
        Context, // A.B
        ArrayElement, // A[x]
        VirtualFunction, // F(...)
        FinalFunction, // F(...)
        IntConst,
        FloatConst,
        StringConst, // "String"
        ObjectConst,
        NameConst, // 'Name'
        RotationConst,
        VectorConst,
        ByteConst,
        IntZero,
        IntOne,
        True,
        False,
        // Formerly IntrinsicParm
        NativeParm, // A            (Native)
        NoObject, // None
        ResizeString,
        IntConstByte, // 0-9          (<= 255)
        BoolVariable, // B            (Bool)
        DynamicCast, // A(B)
        Iterator, // ForEach
        IteratorPop, // Break        (Implied/Explicit)
        IteratorNext, // Continue     (Implied/Explicit)
        StructCmpEq, // A == B
        StructCmpNe, // A != B

        // UnicodeStringConst
        UniStringConst, // "UNICODE"

        RangeConst,
        StructMember, // Struct.Property
        DynArrayLength, // ARRAY.Length
        GlobalFunction, // Global.

        PrimitiveCast, // TYPE(EXPRESSION)

        ReturnNothing,

        DelegateCmpEq,
        DelegateCmpNe,
        DelegateFunctionCmpEq,
        DelegateFunctionCmpNe,
        EmptyDelegate,

        DynArrayInsert,
        DynArrayRemove,
        DebugInfo,
        DelegateFunction,
        DelegateProperty,
        LetDelegate,
        Conditional, // CONDITION ? TRUE_LET : FALSE_LET

        /// <summary>
        /// Find an item within an Array.
        /// </summary>
        DynArrayFind, // ARRAY.Find( EXPRESSION )

        /// <summary>
        /// UE3: Find an item within a struct in an Array.
        /// </summary>
        DynArrayFindStruct, // ARRAY.Find( EXPRESSION, EXPRESSION )

        /// <summary>
        /// UE3: Reference to a property with the Out modifier.
        /// </summary>
        OutVariable,

        /// <summary>
        /// UE3: Default value of a parameter property.
        /// </summary>
        DefaultParmValue, // PARAMETER = EXPRESSION

        /// <summary>
        /// UE3: No parameter value was given e.g: Foo( Foo,, Foo );
        /// </summary>
        EmptyParmValue, // Empty argument, Call(Parm1,,Parm2)
        InstanceDelegate,
        InterfaceContext,
        InterfaceCast,
        
        /// <summary>
        /// Expected to be the last token of every script (UE3).
        /// Older iterations sometimes have an EndCode(a.k.a EatReturnValue) or a FunctionEnd token (different byte-code).
        /// </summary>
        EndOfScript,
        
        DynArrayAdd,
        DynArrayAddItem,
        DynArrayRemoveItem,
        DynArrayInsertItem,
        DynArrayIterator,
        DynArraySort,
        
        /// <summary>
        /// JumpIfNot statement
        /// 
        /// FilterEditorOnly
        /// {
        ///     BLOCK
        /// }
        /// </summary>
        FilterEditorOnly,

        ExtendedNative          = 0x60,
        FirstNative             = 0x70,
        MaxNative               = 0x1000,
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