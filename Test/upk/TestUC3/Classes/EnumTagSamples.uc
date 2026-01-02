// Samples to verify if enum-tag is re-constructed properly using contextual hints.
class EnumTagSamples extends Object;

enum MyEnum
{
    ME_One,
    ME_Two,
};

var MyEnum EnumProperty;

enum OtherEnum
{
    OE_One,
};

var OtherEnum OtherEnumProperty;

function MyEnum EnumTagTest(MyEnum EnumParam = ME_One)
{
    local EnumTagSamples sampler;

    EnumProperty = ME_One;
    self.EnumProperty = ME_One;
    sampler.EnumProperty = ME_One;

    if (EnumProperty == ME_One)
    {
        EnumProperty = ME_One;
    }

    EnumTagTest(ME_One);
    sampler.EnumTagTest(ME_One);

    switch (EnumProperty)
    {
        case ME_One:
            switch (OtherEnumProperty)
            {
                case OE_One: break;
            }
            break;
        // Verify that we don't lose context on the second case, after a nested switch.
        case ME_Two: break;
    }

    return ME_One;
}

