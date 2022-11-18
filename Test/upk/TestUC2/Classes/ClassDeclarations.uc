class ClassDeclarations extends Object;

const Const1 = "";
const Const2 = "";

enum Enum1
{
    E1_Element1,  
    E2_Element2,  
};

struct Struct1
{
    const Const3 = "";

    var enum Enum2
    {
        E2_Element1,
        E2_Element2,
    } Var1;
    
    var int Var2;
};

var int Var3;

delegate Delegate1();
event Event1();

final function int Function1();

final function int Function2(int param1, int param2)
{
    const Const4 = 1;
    return Const4;
}

state State1
{
    begin:
    stop;
}

defaultproperties
{
}
