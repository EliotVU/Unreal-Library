namespace UELib.Tokens
{
    /// <summary>
    /// A collection of tokens describing an expression.
    /// </summary>
    public enum ExprToken : ushort
    {
        /// <summary>
        /// A reference to a local variable (declared in a function)
        /// </summary>
        LocalVariable,

        /// <summary>
        /// A reference to a variable (declared in a class), equivalent to an explicit "self.Field" expression.
        /// </summary>
        InstanceVariable,

        /// <summary>
        /// default.Property
        /// </summary>
        DefaultVariable,

        /// <summary>
        /// A reference to a local variable (declared in a state)
        /// </summary>
        StateVariable,

        /// <summary>
        /// return expr;
        /// </summary>
        Return,

        /// <summary>
        /// switch (c)
        /// </summary>
        Switch,

        /// <summary>
        /// goto offset;
        /// </summary>
        Jump,

        /// <summary>
        /// if (!c) goto offset;
        /// </summary>
        JumpIfNot,

        /// <summary>
        /// stop;
        /// </summary>
        Stop,

        /// <summary>
        /// assert(c);
        /// </summary>
        Assert,
        
        /// <summary>
        /// case/default: expr
        /// </summary>
        Case,

        /// <summary>
        /// May indicate an optional parameter with no default assignment, or an empty argument in a function call.
        /// </summary>
        Nothing,

        /// <summary>
        /// label:
        /// </summary>
        LabelTable,

        /// <summary>
        /// goto 'label' or name expr;
        /// </summary>
        GotoLabel,
        
        ValidateObject,
        EatString,
        EatReturnValue,

        /// <summary>
        /// expr = expr;
        /// </summary>
        Let,

        /// <summary>
        /// DynamicArray[expr]
        /// </summary>
        DynArrayElement,

        /// <summary>
        /// new (expr) expr...
        /// </summary>
        New,

        /// <summary>
        /// expr.expr i.e. Class'Package.Group.Object'.default/static/const.Field
        /// </summary>
        ClassContext,
        
        /// <summary>
        /// <Class>(expr)
        /// </summary>
        MetaCast,

        /// <summary>
        /// Contains the size of elements in the function's stack.
        /// </summary>
        BeginFunction,
        
        /// <summary>
        /// expr = expr;
        /// </summary>
        LetBool,

        /// <summary>
        /// Marks the current script line number.
        /// </summary>
        LineNumber,

        /// <summary>
        /// Indicates the end of a default parameter assignment.
        /// </summary>
        EndParmValue,

        /// <summary>
        /// Indicates the end of a function call.
        /// </summary>
        EndFunctionParms,

        /// <summary>
        /// self
        /// </summary>
        Self,

        /// <summary>
        /// Marks a code size for the VM to skip, used in operators to skip a redundant conditional check.
        /// </summary>
        Skip,

        /// <summary>
        /// expr.expr
        /// </summary>
        Context,

        /// <summary>
        /// FixedArray[expr]
        /// </summary>
        ArrayElement,

        /// <summary>
        /// Object.VirtualFunction(params...)
        /// </summary>
        VirtualFunction,

        /// <summary>
        /// Object.FinalFunction(params...)
        /// </summary>
        FinalFunction,

        /// <summary>
        /// 0xFFFFFFFF
        /// </summary>
        IntConst,

        /// <summary>
        /// 0.0f
        /// </summary>
        FloatConst,

        /// <summary>
        /// "String"
        /// </summary>
        StringConst,

        /// <summary>
        /// Class'Package.Group.Object'
        /// </summary>
        ObjectConst,

        /// <summary>
        /// 'Name'
        /// </summary>
        NameConst,

        /// <summary>
        /// rot(0, 0, 0)
        /// </summary>
        RotationConst,

        /// <summary>
        /// vect(0f, 0f, 0f)
        /// </summary>
        VectorConst,

        /// <summary>
        /// 0xFF
        /// </summary>
        ByteConst,

        /// <summary>
        /// 0
        /// </summary>
        IntZero,

        /// <summary>
        /// 1
        /// </summary>
        IntOne,

        /// <summary>
        /// true
        /// </summary>
        True,

        /// <summary>
        /// false
        /// </summary>
        False,

        /// <summary>
        /// A reference to a native/intrinsic parameter.
        /// </summary>
        NativeParm,

        /// <summary>
        /// none
        /// </summary>
        NoObject,

        /// <summary>
        /// (string)
        /// </summary>
        ResizeString,

        /// <summary>
        /// byte(0xFF)
        /// </summary>
        IntConstByte,

        /// <summary>
        /// (bool)
        /// </summary>
        BoolVariable,

        /// <summary>
        /// Class(Expr)
        /// </summary>
        DynamicCast,
        
        /// <summary>
        /// foreach expr
        /// </summary>
        Iterator,
        
        /// <summary>
        /// break;
        /// </summary>
        IteratorPop,
        
        /// <summary>
        /// continue;
        /// </summary>
        IteratorNext,

        /// <summary>
        /// A == B
        /// </summary>
        StructCmpEq,
        
        /// <summary>
        /// A != B
        /// </summary>
        StructCmpNe, 

        StructConst,

        /// <summary>
        /// "Unicode characters"
        /// </summary>
        UnicodeStringConst,

        /// <summary>
        /// rng(a, b)
        /// </summary>
        RangeConst,

        /// <summary>
        /// Struct.Member
        /// </summary>
        StructMember,

        /// <summary>
        /// Array.Length
        ///
        /// aka DynArrayCount
        /// </summary>
        DynArrayLength,

        /// <summary>
        /// global.VirtualFunction(params...)
        /// </summary>
        GlobalFunction,

        /// <summary>
        /// primitiveKeyword(Expr)
        /// </summary>
        PrimitiveCast,

        /// <summary>
        /// return;
        /// </summary>
        ReturnNothing,

        /// <summary>
        /// expr == expr
        /// </summary>
        DelegateCmpEq,
        
        /// <summary>
        /// expr != expr
        /// </summary>
        DelegateCmpNe,

        /// <summary>
        /// expr == expr
        /// </summary>
        DelegateFunctionCmpEq,

        /// <summary>
        /// expr != expr
        /// </summary>
        DelegateFunctionCmpNe,

        /// <summary>
        /// none
        /// </summary>
        EmptyDelegate,

        /// <summary>
        /// DynamicArray.Insert(...)
        /// </summary>
        DynArrayInsert,
        
        /// <summary>
        /// DynamicArray.Remove(...)
        /// </summary>
        DynArrayRemove,

        /// <summary>
        /// Marks the current line number and the control context.
        /// </summary>
        DebugInfo,

        /// <summary>
        /// A reference to a declared delegate function.
        /// </summary>
        DelegateFunction,

        /// <summary>
        /// A reference to to a declared delegate property.
        /// </summary>
        DelegateProperty,

        /// <summary>
        /// expr = expr;
        /// </summary>
        LetDelegate,

        /// <summary>
        /// expr ? expr : expr
        /// </summary>
        Conditional,

        /// <summary>
        /// Array.Find(expr)
        /// </summary>
        DynArrayFind,

        /// <summary>
        /// ArrayOfStructs.Find(expr, expr)
        /// </summary>
        DynArrayFindStruct,

        /// <summary>
        /// A reference to an out variable
        ///
        /// <example>
        /// (out int parameter)
        /// </example>
        /// </summary>
        OutVariable,

        /// <summary>
        /// (int parameter = expr)
        /// </summary>
        DefaultParmValue,

        /// <summary>
        /// Indicates an empty argument was passed to a function call e.g. Call(1,, 1)
        /// </summary>
        EmptyParmValue,

        /// <summary>
        /// A reference to a delegate (but by name).
        /// </summary>
        InstanceDelegate,

        /// <summary>
        /// expr.expr
        /// </summary>
        InterfaceContext,

        /// <summary>
        /// InterfaceClass(expr)
        /// </summary>
        InterfaceCast,

        /// <summary>
        /// 0xFFFF
        /// </summary>
        PointerConst,

        /// <summary>
        /// Indicates the end of the script.
        /// </summary>
        EndOfScript,

        /// <summary>
        /// DynamicArray.Add(expr)
        /// </summary>
        DynArrayAdd,
        
        /// <summary>
        /// DynamicArray.AddItem(expr)
        /// </summary>
        DynArrayAddItem,

        /// <summary>
        /// DynamicArray.AddItem(expr)
        /// </summary>
        DynArrayRemoveItem,

        /// <summary>
        /// DynamicArray.InsertItem(expr, expr)
        /// </summary>
        DynArrayInsertItem,

        /// <summary>
        /// foreach expr (expr, expr?)
        /// </summary>
        DynArrayIterator,

        /// <summary>
        /// DynamicArray.Sort(expr)
        /// </summary>
        DynArraySort,

        /// <summary>
        /// DynamicArray.Empty(expr?)
        /// </summary>
        DynArrayEmpty,
        
        /// <summary>
        /// JumpIfNot statement
        /// 
        /// FilterEditorOnly
        /// {
        ///     BLOCK
        /// }
        /// </summary>
        FilterEditorOnly,

        NativeFunction,

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
