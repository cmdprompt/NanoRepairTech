using System;
using Verse;

namespace Ogre.NanoRepairTech
{
	internal sealed class SupportedBed
	{
		internal SupportedBed()
		{
			this.ModName = null;
			this.DefName = null;
			this.UseHospitalLinkablesOnly = false;
			this.FnPostProcess = null;
		}

		internal SupportedBed(string modName, string defName, bool useHospitalLinksablesOnly = false, Action<ThingDef> fnPostProcess = null) : this()
		{
			this.ModName = modName;
			this.DefName = defName;
			this.UseHospitalLinkablesOnly = useHospitalLinksablesOnly;
			this.FnPostProcess = fnPostProcess;
		}

		public string ModName { get; set; }
		public string DefName { get; set; }
		public bool UseHospitalLinkablesOnly { get; set; }
		public Action<ThingDef> FnPostProcess { get; set; }
	}
}
