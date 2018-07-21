using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ogre.NanoRepairTech
{
	public class TickData
	{
		public TickData() { }

		// increment cannot be stored like it used to
		// because tick data will be wrong after its generated
		// from low tier to advanced tier
		//public float Increment;
		public float Accumulated;
		public float TickAmount;

		public bool AddHP(float increment)
		{
			this.Accumulated = this.Accumulated + this.TickAmount;
			if (this.Accumulated >= increment)
			{
				this.Accumulated = this.Accumulated - increment;
				return true;
			}
			return false;
		}
	}
}
