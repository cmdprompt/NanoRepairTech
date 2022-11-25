using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class NanoShelf : Building_Storage, INano
	{
		private NanoRepair _nano;
		public NanoShelf() {
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

			List<Thing> apparel = new List<Thing>();
			List<Thing> weapons = new List<Thing>();

			if (_nano.CmpPowerTrader.PowerOn && _nano.CmpRefuelable.HasFuel)
			{
				SlotGroup group = base.GetSlotGroup();
				if (group != null && group.HeldThings != null)
				{
					bool weaponsComplete = NanoRepair.IsWeaponResearchComplete();
					foreach (Thing thing in group.HeldThings)
					{
						if (thing.def != null && thing.def.stackLimit == 1 && !thing.def.Minifiable)
						{
							if (thing.def.IsApparel)
							{
								apparel.Add(thing);
							}
							else if (weaponsComplete && (thing.def.IsMeleeWeapon || thing.def.IsRangedWeapon))
							{
								weapons.Add(thing);
							}
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

			// this appears to take a huge shit on itself
			// if offered as a quest reward, presumably because
			// the same code paths are not followed? when
			// checking to see if it's spawned first seems to fix it
			if (Scribe.mode == LoadSaveMode.Saving && this.Spawned)
			{
				SlotGroup group = this.GetSlotGroup();
				if (group != null && group.HeldThings != null)
				{
					_nano.CleanUpTickTracker(new List<Thing>(group.HeldThings)
						.Where(x => x != null)
						.Select(x => x.thingIDNumber)
					);
				}
			}
			_nano.Persist();
		}

		//===============================================================================\\

		public TickData GenerateTickData()
		{
			return new TickData()
			{
				Accumulated = 0,
				TickAmount = 250 * (1 + (_nano.DetermineQualityModifier() - 1))
			};
		}

		//===============================================================================\\

		public float RareTicksPerYear()
		{
			// unit * tick rares
			// 250 * 14400
			return 3600000f;
		}
	}
}
