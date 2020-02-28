using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Ogre.NanoRepairTech
{
	public class NanoBed : RimWorld.Building_Bed, INano
	{
		// arbitrary unit * tick rares
		// 250 * 4800
		//private static readonly float RARE_TICKS_PER_YEAR = 1200000f;

		private NanoRepair _nano;

		public NanoBed()
		{
			_nano = new NanoRepair();
		}

		//===============================================================================\\

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);

			_nano.Setup(this);
		}

		//===============================================================================\\

		public override void TickRare()
		{
			base.TickRare();

			List<Apparel> apparel = new List<Apparel>();
			List<Thing> weapons = new List<Thing>();

			if (_nano.CmpPowerTrader.PowerOn && _nano.CmpRefuelable.HasFuel)
			{
				foreach (Pawn occupant in new List<Pawn>(this.CurOccupants).Where(x => x != null))
				{
					if (occupant.apparel != null && occupant.apparel.WornApparel != null)
						apparel.AddRange(new List<Apparel>(occupant.apparel.WornApparel).Where(x => x != null && x.def != null));

					if (NanoRepair.IsWeaponResearchComplete())
					{
						if (occupant.equipment != null && occupant.equipment.GetDirectlyHeldThings() != null)
						{
							weapons.AddRange(new List<Thing>(occupant.equipment.GetDirectlyHeldThings()).Where(x =>
							{
								return (x != null)
									&& (x.def != null)
									&& (x.def.IsRangedWeapon || x.def.IsMeleeWeapon);
							}));
						}
					}
				}
			}

			_nano.ProcessTick(apparel, weapons, this);
		}

		//===============================================================================\\

		public override void ExposeData()
		{
			base.ExposeData();

			if (Scribe.mode == LoadSaveMode.Saving && this.Spawned)
			{
				List<int> possibleIDs = new List<int>();
				foreach (Pawn p in new List<Pawn>(this.AssignedPawns).Where(x => x != null))
				{
					if (p.apparel != null)
					{
						foreach (Apparel a in new List<Apparel>(p.apparel.WornApparel).Where(x => x != null))
						{
							possibleIDs.Add(a.thingIDNumber);
						}
					}

					if (p.equipment != null)
					{
						foreach (Thing w in new List<Thing>(p.equipment.GetDirectlyHeldThings()).Where(x => x != null))
						{
							possibleIDs.Add(w.thingIDNumber);
						}
					}
				}

				_nano.CleanUpTickTracker(possibleIDs);

			}
			_nano.Persist();
		}

		//===============================================================================\\

		public TickData GenerateTickData()
		{
			float restEffectiveness = this.GetStatValue(StatDefOf.BedRestEffectiveness, false);
			return new TickData()
			{
				Accumulated = 0,
				TickAmount = 250 * (1 + (restEffectiveness - 1) + (_nano.DetermineQualityModifier() - 1))
			};
		}

		//===============================================================================\\

		public float RareTicksPerYear()
		{
			// arbitrary unit * tick rares
			// 250 * 4800
			return 1200000f;
		}

	}
}
