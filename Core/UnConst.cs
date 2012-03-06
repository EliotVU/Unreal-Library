using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal const. 
	/// </summary>
	public partial class UConst : UField
	{
		#region Serialized Members
		/// <summary>
		/// Constant Value
		/// </summary>
		public string Value
		{
			get;
			private set;
		}
		#endregion

		/// <summary>
		/// Creates a new instance of the UELib.Core.UConst class. 
		/// </summary>
		public UConst()
		{
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			// Size:BYTES:\0
			Value = _Buffer.ReadName();
		}
	}
}
