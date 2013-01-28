namespace UELib.Tokens
{
    /// <summary>
    /// A collection of tokens describing a expression.
    /// 
    /// The redefined tokens are not valid if the past token expression was a PrimitiveCast/RotatorToVector expression.
    /// </summary>
    public enum ExprToken : byte
    {
        /// UNTACKLED TOKENS
        /// ValidateObject
        ///	ResizeString	
        ///	BeginFunction	
        
        LocalVariable			= 0x00,
        InstanceVariable		= 0x01,
        DefaultVariable			= 0x02,
        // GAP:					= 0x03,
        Return					= 0x04,
        Switch					= 0x05,
        Jump					= 0x06,		// goto CODEOFFSET
        JumpIfNot				= 0x07,		// if( !CONDITION ) goto CODEOFFSET;
        Stop					= 0x08,		// Stop			(State)
        Assert					= 0x09,
        Case 					= 0x0A,
        Nothing					= 0x0B,
        LabelTable				= 0x0C,
        GotoLabel				= 0x0D,
        EatString				= 0x0E,
        Let						= 0x0F,		// A = B
        DynArrayElement			= 0x10,
        New 					= 0x11,		// new(OUTER) CLASS...
        ClassContext 			= 0x12,
        MetaCast 				= 0x13,		// <CLASS>(CLASS)
        LetBool 				= 0x14,
        EndParmValue			= 0x15,
        EndFunctionParms 		= 0x16,		// )
        Self 					= 0x17,		// Self
        Skip 					= 0x18,
        Context 				= 0x19,		// A.B
        ArrayElement 			= 0x1A,		// A[x]
        VirtualFunction 		= 0x1B,		// F(...)
        FinalFunction 			= 0x1C,		// F(...)

        IntConst 				= 0x1D,
        FloatConst 				= 0x1E,
        StringConst 			= 0x1F,		// "String"
        ObjectConst 			= 0x20,
        NameConst 				= 0x21,		// 'Name'
        RotatorConst 			= 0x22,
        VectorConst 			= 0x23,
        ByteConst 				= 0x24,

        IntZero 				= 0x25,
        IntOne 					= 0x26,
        True 					= 0x27,
        False 					= 0x28,
        NativeParm 				= 0x29,		// A			(Native)
        NoObject 				= 0x2A,		// None

        // GAP:					= 0x2B,
        IntConstByte 			= 0x2C,		// 0-9			(<= 255)
        BoolVariable 			= 0x2D,		// B			(Bool)
        DynamicCast 			= 0x2E,		// A(B)	

        Iterator 				= 0x2F,		// ForEach
        IteratorPop 			= 0x30,		// Break		(Implied/Explicit)
        IteratorNext 			= 0x31,		// Continue		(Implied/Explicit)

        StructCmpEq 			= 0x32,		// A == B
        StructCmpNE 			= 0x33,		// A != B
        UniStringConst 			= 0x34,		// "UNICODE"

        #region FixedByteCodes
            // Note:In Unreal Engine 3 all tokens >= 0x35 are one value lower than represented here to remain compatible with older engine generations.
            // Be aware of CastTokens it applies there as well.
            // This means that you should increment the read token value, to get it compatible with Unreal Engine 3 bytecode.
            Unknown					= 0x35,

            StructMember 			= 0x36,
            DynArrayLength			= 0x37,		// ARRAY.Length
            GlobalFunction			= 0x38,		// Global.
                
            /// <summary>
            /// Redefined(RotatorToVector)
            /// 
            /// UE1:RotatorToVector	cast.
            /// UE2+:Meaning the next bytecode token is the cast version rather than the other meaning.
            /// </summary>
            PrimitiveCast 			= 0x39,		// TYPE(EXPRESSION)

            // Note:As said before all 0x35 are one value lesser in Unreal Engine 3, however this stops here up to NoDelegate.
        #endregion	// FixedByteCodes
        /// <summary>
        /// Redefined(DynArrayRemove)
        /// 
        /// UE1:ByteToInt cast.
        /// UE2:ReturnNothing
        /// UE3:ReturnNothing if previous token is a ReturnToken, DynArrayRemove when not.
        /// </summary>
        ReturnNothing			= 0x3A,		// Redefined, 

        #region UE3ByteCodes
            DelegateCmpEq			= 0x3B,	
            DelegateCmpNE			= 0x3C,	
            DelegateFunctionCmpEq	= 0x3D,		
            DelegateFunctionCmpNE	= 0x3E,		
            NoDelegate				= 0x3F,		

            #region FixedByteCodes
                // Note:In UE1, UE2 these values are as what they are set here, but in UE3 they are one number lower!
                DynArrayInsert			= 0x40,	// Or 0x3F in UE3 (primitive is 0x3E then thus a gap occurrs, see above).
                DynArrayRemove 			= 0x41, // Or 0x40 in UE3.
                DebugInfo				= 0x42,	
                DelegateFunction		= 0x43,
                DelegateProperty		= 0x44,
                LetDelegate 			= 0x45,

                /// <summary>
                /// An alternative to Else-If statements using A ? B : C;.
                /// </summary>
                Conditional				= 0x46,		// CONDITION ? TRUE_LET : FALSE_LET

                /// <summary>
                /// Find a item within an Array.
                /// </summary>
                DynArrayFind			= 0x47,		// ARRAY.Find( EXPRESSION )

                /// <summary>
                /// Redefined(ObjectToBool,DynArrayFind)
                /// 
                /// UE1:As a ObjectToBool cast.
                /// UE2:As a indication of function end(unless preceded by PrimitiveCast then it is treat as a ObjectToBool).
                /// UE3:See DynArrayFind(See EndOfScript).
                /// </summary>
                FunctionEnd				= 0x47,

                /// <summary>
                /// Find a item within a struct in an Array.
                /// </summary>
                DynArrayFindStruct		= 0x48,		// ARRAY.Find( EXPRESSION, EXPRESSION )

                /// <summary>
                /// In some Unreal Engine 2 games, see Conditional for Unreal Engine 3.
                /// 
                /// An alternative to Else-If statements using A ? B : C;.
                /// </summary>
                Eval					= 0x48,		// See Conditional

                /// <summary>
                /// Reference to a property with the Out modifier.
                /// </summary>
                OutVariable				= 0x49,
    
                /// <summary>
                /// Default value of a parameter property.
                /// </summary>
                DefaultParmValue		= 0x4A,		// PARAMETER = EXPRESSION

                /// <summary>
                ///	No parameter value was given e.g: Foo( Foo,, Foo ); 
                /// </summary>
                NoParm					= 0x4B,		// ,...,

                // EmptyParmValue

                InstanceDelegate		= 0x4C,

                #region CustomTokens
                    VarInt					= 0x4D,		// Found in Borderlands 2
                    VarFloat				= 0x4E,		// Found in Borderlands 2
                    VarByte					= 0x4F,		// Found in Borderlands 2
                    VarBool					= 0x50,		// Found in Borderlands 2
                    VarObject				= 0x51,		// Found in Borderlands 2

                    StringRef				= 0x50,		// Found in Mirrors Edge
                    UndefinedVariable		= 0x51,		// Found in Gears of War
                #endregion	// CustomTokens

                InterfaceContext		= 0x52,
                InterfaceCast			= 0x53,

                EndOfScript				= 0x54,

                DynArrayAdd				= 0x55,
                DynArrayAddItem			= 0x56,
                DynArrayRemoveItem		= 0x57,
                DynArrayInsertItem		= 0x58,	
                DynArrayIterator		= 0x59,
                DynArraySort			= 0x5A,
                FilterEditorOnly		= 0x5B,
            #endregion	// FixedByteCodes
        #endregion	// UE3ByteCodes

        // Natives.
        ExtendedNative			= 0x60,
        FirstNative				= 0x70,
        //S						= 0xF80,
        //0x80
    }

    /// <summary>
    /// A collection of tokens describing a cast.
    /// 
    /// The redefined tokens are only valid if the past token expression was PrimitiveCast/RotatorToVector.
    /// </summary>
    public enum CastToken : byte
    {
        None					= 0x00,
        #region UE3ByteCodes
            InterfaceToBool			= 0x36,
            InterfaceToString		= 0x37,		// ???
            InterfaceToObject       = 0x38,
        #endregion

        RotatorToVector 		= 0x39,		// Redefined
        ByteToInt 				= 0x3A,		// Redefined(ReturnNothing)
        ByteToBool 				= 0x3B,
        ByteToFloat 			= 0x3C,
        IntToByte 				= 0x3D,
        IntToBool 				= 0x3E,
        IntToFloat 				= 0x3F,	
        BoolToByte 				= 0x40,		// Redefined  
        BoolToInt 				= 0x41,		// Redefined
        BoolToFloat 			= 0x42,	   	// Redefined
        FloatToByte 			= 0x43,		// Redefined
        FloatToInt 				= 0x44,	 	// Redefined
        FloatToBool 			= 0x45,	 	// Redefined
        ObjectToInterface		= 0x46,
        ObjectToBool 			= 0x47,		// Redefined
        NameToBool 				= 0x48,		// Redefined
        StringToByte 			= 0x49,
        StringToInt 			= 0x4A,
        StringToBool 			= 0x4B,
        StringToFloat 			= 0x4C,
        StringToVector 			= 0x4D,
        StringToRotator 		= 0x4E,
        VectorToBool 			= 0x4F,
        VectorToRotator 		= 0x50,
        RotatorToBool 			= 0x51,
        ByteToString 			= 0x52,
        IntToString 			= 0x53,
        BoolToString 			= 0x54,
        FloatToString 			= 0x55,
        ObjectToString 			= 0x56,
        NameToString 			= 0x57,
        VectorToString 			= 0x58,
        RotatorToString 		= 0x59,
        DelegateToString        = 0x5A,
        StringToName 			= 0x60,
    }

    public enum DebugInfo
    {
        Let					= 0x00,
        SimpleIf			= 0x01,
        Switch				= 0x02,
        While				= 0x03,
        Assert				= 0x04,
        Return				= 0x10,
        ReturnNothing		= 0x11,
        NewStack			= 0x20,
        NewStackLatent		= 0x21,
        NewStackLabel		= 0x22,
        PrevStack			= 0x30,
        PrevStackLatent		= 0x31,
        PrevStackLabel		= 0x32,
        PrevStackState		= 0x33,
        EFP					= 0x40,
        EFPOper				= 0x41,
        EFPIter				= 0x42,
        ForInit				= 0x50,
        ForEval				= 0x51,
        ForInc				= 0x52,
        BreakLoop			= 0x60,
        BreakFor			= 0x61,
        BreakForEach		= 0x62,
        BreakSwitch			= 0x63,
        ContinueLoop		= 0x70,
        ContinueForeach		= 0x71,
        ContinueFor			= 0x72,
    }
}
