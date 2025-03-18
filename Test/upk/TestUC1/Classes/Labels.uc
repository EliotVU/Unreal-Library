class Labels extends Object;

// We use the assert statement, i.e. "assert (condition);" here as a code filler.

function TestSwitchNesting()
{
	switch (true)
	{
		case true:
		case false:
		case true:
			break;

		case false:
			break;

		default:
			break;
	}
}

/**
 * Test jump nesting case where a switch with a default case is missing a close of its outer nest.
 */
function TestIfAndSwitchWithDefaultNesting()
{
	if (true) // We are testing for this nesting block.
	{
		if (true)
		{
			switch (true)
			{
				case true:
					assert (true);
				default:
					assert (true);
			}
		}
	}

	if (true)
	{
		return;
	}
}

function TestIfAndSwitchWithEmptyNesting()
{
	if (true)
	{
		if (true)
		{
			switch (true)
			{
				// FIXME: With empty rules, we run into decompile issues
				case true:
				default:
			}
		}
	}

	if (true)
	{
		return;
	}
}

// Issue: The if is closed with along with a nesting block.
function TestSwitchAndCaseWithIfNesting()
{
	switch (true)
	{
		case true:
			if (true)
			{
				assert (true);
				break;
			}

		default:
			return;
	}
}

function TestSwitchAndCaseWithLabels()
{
	switch (true)
	{
		case true:
			if (true)
			{
				assert (true);
				goto Case2;
			}
			break;

		case false:
			if (true)
			{
				assert (true);
				break;
			}
			Case2:

		default:
			assert (true);
	}
}

function TestForAndIfWithElse()
{
	local int i;

	for (i = 0; i < 0xFF; i++)
	{
		if (true)
		{
			if (false)
			{
				assert (true);
			}
			else
			{
				assert (true);
				if (false)
				{
					assert (true);
					continue;
				}
			}
		}
		assert (true);
	}
	assert (true);
}

function TestIfWithGoto()
{
	if (true)
	{
		assert (true);
		if (true)
		{
			assert (true);
			if (true)
			{
				assert (true);
				goto NextLabel;
			}
		}
	}

	NextLabel:
	if (false)
	{
	}
	else
	{
		assert (true);
		if (false)
		{
			assert (true);
		}
		else
		{
			assert (false);
		}
	}
}

function TestIfAndWhileLoopLabel()
{
	if (false)
	{
	}
	else
	{
	}

	// We expect a loop label here.
	while (true)
	{
	}
}

function TestForLoop()
{
	local int i;

	for (i = 0; i < 0xFF; ++i)
	{
		assert (false);
	}
}

function TestWhileLoop()
{
	while (true)
	{
		assert (false);
	}
}

function TestDoUntilLoop()
{
	do
	{
		assert (true);
	} until (true);
}