using UELib.Core;

namespace UELib.Engine
{
	/// <summary>
	/// Any view/extract related classes should subclass this.
	/// </summary>
	public class UContent : UObject, IUnrealViewable
	{
		public UContent()
		{
			ShouldDeserializeOnDemand = true;
		}

		public virtual void View()
		{
		}
	}

	// Package reference or Group
	public class UPackage : UObject
	{
	}

	public class UModel : UContent
	{
	}
	
	public class USound : UContent
	{
	}
}
