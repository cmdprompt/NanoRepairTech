using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class CompUseEffect_NanoTechHealScar : CompUseEffect
	{
		public override void DoEffect(Pawn usedBy)
		{
			if (usedBy != null && !usedBy.Dead && usedBy.health != null && usedBy.health.hediffSet != null && usedBy.health.hediffSet.hediffs != null)
			{
				List<Hediff> heDiffs = new List<Hediff>(usedBy.health.hediffSet.hediffs);
				List<Hediff> permanent = new List<Hediff>();

				Dictionary<string, Hediff_NanoTechHealScar> existing = new Dictionary<string, Hediff_NanoTechHealScar>();

				for (int i = 0; i < heDiffs.Count; i++)
				{
					Hediff_Injury inj = heDiffs[i] as Hediff_Injury;
					if (inj != null && inj.IsPermanent())
					{
						permanent.Add(heDiffs[i]);
						continue;
					}

					Hediff_NanoTechHealScar healing = heDiffs[i] as Hediff_NanoTechHealScar;
					if (healing != null)
					{
						existing.Add(healing.healingID, healing);
					}
				}

				for (int i = 0; i < permanent.Count; i++)
				{
					if (!existing.ContainsKey(permanent[i].GetUniqueLoadID()))
					{
						//HediffMaker.MakeHediff()
						HediffDef d = DefDatabase<HediffDef>.AllDefsListForReading.First<HediffDef>(x => x.defName == "Ogre_NanoTech_HeDiffHealScar");
						Hediff_NanoTechHealScar df = (Hediff_NanoTechHealScar)HediffMaker.MakeHediff(d, usedBy, null);
						//60,000 ticks per day
						// 1 severity per day
						int healAfter = (int)(permanent[i].Severity * 60000);
						df.ticksTotal = healAfter < 60000 ? 60000 : healAfter;

						df.healingID = permanent[i].GetUniqueLoadID();
						usedBy.health.AddHediff(df);
					}
				}
			}

			base.DoEffect(usedBy);
		}
	}
}
