class Labels extends Object;

/**
 * Test jump nesting case where a switch with a default case is missing a close of its outer nest.
 */
function TestIfAndSwitchWithDefaultNesting()
{
	if(true) // We are testing for this nesting block.
	{
		if(true)
		{
			switch(true)
			{
				case true:
				default:
			}
		}
	}

	if(true)
	{
		return;
	}
}

// Issue: The if is closed with along with a nesting block.
function TestSwitchAndCaseWithIfNesting()
{
	switch(true)
	{
		case true:
			if(true)
			{
				assert(true);
				break;
			}

		default:
			return;
	}
}

function TestSwitchAndCaseWithLabels()
{
	switch(true)
	{
		case true:
			if(true)
			{
				assert(true);
				goto Case2;
			}
			break;

		case false:
			if(true)
			{
				assert(true);
				break;
			}
			Case2:

		default:
			assert(true);
	}
}

function TestForAndIfWithElse()
{
	local int i;

	for(i = 0; i < 0xFF; i++)
	{
		if(true)
		{
			if(false)
			{
				assert(true);
			}
			else
			{
				assert(true);
				if(false)
				{
					assert(true);
					continue;
				}
			}
		}
		assert(true);
	}
	assert(true);
}

function TestIfWithGoto()
{
	if(true)
	{
		assert(true);
		if(true)
		{
			assert(true);
			if(true)
			{
				assert(true);
				goto NextLabel;
			}
		}
	}

	NextLabel:
	if(false)
	{
	}
	else
	{
		assert(true);
		if(false)
		{
			assert(true);
		}
		else
		{
			assert(false);
		}
	}
}