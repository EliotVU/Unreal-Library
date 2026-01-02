class PrecedenceSamples extends Object;

// '-' Should precede
function float BinaryOperatorPrecedenceTest()
{
    return 1.0f * (1.0 - 1.0);
}

// '!' should output parenthesises around the '&&' operator.
function bool PreOperatorPrecedenceTest()
{
    return !(true == true && false == false);
}

// No suitable post operator exists in UT2004.
// No test
