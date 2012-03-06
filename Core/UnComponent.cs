using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib.Core
{
	using UELib;

	public class UComponent : UObject
	{
		protected override void Deserialize()
		{
			_Buffer.ReadInt32();
			_Buffer.ReadNameIndex();
			base.Deserialize();
		}
	}
}
