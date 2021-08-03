using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class Hediff_NanoTechHealScar : HediffWithComps
	{
		public int ticksTotal = 0;
		public string healingID = string.Empty;

		private Hediff_Injury scar = null;

		private Hediff_Injury getScar()
		{
			if (scar == null)
			{
				List<Hediff> hediffs = new List<Hediff>(this.pawn.health.hediffSet.hediffs);
				for (int i = 0; i < hediffs.Count; i++)
				{
					if (hediffs[i].GetUniqueLoadID() == healingID)
					{
						scar = hediffs[i] as Hediff_Injury;
					}
				}
			}
			return scar;
		}

		public Hediff_NanoTechHealScar() {
			
		}

		public override void Tick()
		{
			--ticksTotal;
			if (ticksTotal % 2500 == 0)
			//if (ticksTotal % 250 == 0)
			{
				Hediff_Injury inj = this.getScar();
				if (inj == null)
				{
					this.remove();
				}
				else
				{
					inj.Severity -= 0.041666666f;
					if (inj.Severity <= 0)
						this.remove();
				}
			}

			if (ticksTotal <= 0)
			{
				this.remove();
			}

			//base.Tick();
		}
		
		private void remove()
		{
			Verse.Log.Message("Remove.");
			Hediff_Injury scar = this.getScar();
			if (scar != null)
			{
				this.pawn.health.RemoveHediff(scar);
			}

			// this makes it crash
			//this.pawn.health.RemoveHediff(this);

			// it apparently goes away on its own
			// if you set the severity to 0
			this.Severity = 0;
			
		}

		public override void ExposeData()
		{
			Scribe_Values.Look<int>(ref this.ticksTotal, "ticksTotal", 0, true);
			Scribe_Values.Look<string>(ref this.healingID, "healingID", string.Empty, true);
			base.ExposeData();
		}
	}
}
