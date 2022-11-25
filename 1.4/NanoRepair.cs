using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using System.Reflection;

namespace Ogre.NanoRepairTech
{
    public class NanoRepair
    {
		private bool _isLoaded = false;

		public Dictionary<int, TickData> TickTracker;

		public CompPowerTrader CmpPowerTrader;
		public CompQuality CmpQuality;
		public CompRefuelable CmpRefuelable;

		public StatModifier StatModApparel;
		public StatModifier StatModWeapons;

		internal readonly static float HP_APPAREL_PER_YEAR = 624f;
		internal readonly static float HP_WEAPON_PER_YEAR = 400f;

		private readonly static float LOW_TIER_APPAREL = 0.6f;
		private readonly static float LOW_TIER_WEAPONS = 0.5f;
		private readonly static float LOW_TIER_POWER = 0.75f;

		private readonly static float FUEL_RATE = 0.2f;

		private static ResearchProjectDef _researchDefWeapons = null;
		private static ResearchProjectDef _researchDefAdvanced = null;
		private static readonly object _lock = new object();

		//===============================================================================\\

		public NanoRepair() {
			this.TickTracker = new Dictionary<int, TickData>();
		}

		//===============================================================================\\

		public void Setup(ThingWithComps nano)
		{
			this.CmpPowerTrader = nano.GetComp<CompPowerTrader>();
			this.CmpQuality = nano.GetComp<CompQuality>();
			this.CmpRefuelable = nano.GetComp<CompRefuelable>();

			this.StatModApparel = nano.def.statBases
				.Find(x => x.stat.defName == "Ogre_NanoApparelRate");

			this.StatModWeapons = nano.def.statBases
				.Find(x => x.stat.defName == "Ogre_NanoWeaponsRate");
		}

		//===============================================================================\\

		public void ProcessTick(List<Thing> apparel, List<Thing> weapons, INano nanoObject)
		{
			// Stat Defs for UI
			bool isAdvancedComplete = IsAdvancedResearchComplete();
			bool isWeaponsComplete = IsWeaponResearchComplete();

			this.StatModApparel.value = isAdvancedComplete
				? 100
				: (100 * LOW_TIER_APPAREL);

			if (isWeaponsComplete)
			{
				this.StatModWeapons.value = isAdvancedComplete
					? 100
					: (100 * LOW_TIER_WEAPONS);
			}
			else
			{
				this.StatModWeapons.value = 0;
			}

			// Apparel / Weapons

			int powerApparelCount = 0;
			int powerWeaponsCount = 0;
			int tickFuel = 0;

			if (this.CmpPowerTrader.PowerOn && this.CmpRefuelable.HasFuel)
			{
				foreach (Apparel a in apparel)
				{
					if (a.HitPoints < a.MaxHitPoints)
					{
						++powerApparelCount;
						TickData data = this.getTickData(a.thingIDNumber, nanoObject);
						if (data.AddHP(NanoRepair.GetIncrementApparel(a, nanoObject)))
						{
							a.HitPoints = a.HitPoints + 1;
							++tickFuel;
							if (a.HitPoints >= a.MaxHitPoints)
							{
								this.TickTracker.Remove(a.thingIDNumber);
								--powerApparelCount;
							}
						}
					}

					if (a.WornByCorpse && a.HitPoints >= (a.MaxHitPoints * 0.98))
					{
						FieldInfo info = a.GetType().GetField("wornByCorpseInt", BindingFlags.Instance | BindingFlags.NonPublic);
						info.SetValue(a, false);
					}
				}

				if (isWeaponsComplete)
				{
					foreach (Thing w in weapons)
					{
						if (w != null && w.def != null && (w.def.IsRangedWeapon || w.def.IsMeleeWeapon))
						{
							if (w.HitPoints < w.MaxHitPoints)
							{
								++powerWeaponsCount;
								TickData wData = this.getTickData(w.thingIDNumber, nanoObject);
								if (wData.AddHP(NanoRepair.GetIncrementWeapon(w, nanoObject)))
								{
									w.HitPoints = w.HitPoints + 1;
									++tickFuel;
									if (w.HitPoints >= w.MaxHitPoints)
									{
										this.TickTracker.Remove(w.thingIDNumber);
										--powerWeaponsCount;
									}
								}
							}
						}
					}
				}
			}

			if (powerApparelCount == 0 && powerWeaponsCount == 0)
			{
				this.CmpPowerTrader.PowerOutput = -(this.CmpPowerTrader.Props.PowerConsumption * 0.1f);
			}
			else
			{
				float factor = this.CmpPowerTrader.Props.PowerConsumption * (isAdvancedComplete ? 1.0f : LOW_TIER_POWER);
				this.CmpPowerTrader.PowerOutput = -(factor * (powerApparelCount + (2 * powerWeaponsCount)));
			}

			if (tickFuel > 0)
			{
				this.CmpRefuelable.ConsumeFuel(FUEL_RATE * tickFuel);
			}
		}

		//===============================================================================\\

		private TickData getTickData(int thingID, INano nanoObj)
		{
			TickData data = null;
			if (!this.TickTracker.TryGetValue(thingID, out data))
			{
				data = nanoObj.GenerateTickData();
				this.TickTracker.Add(thingID, data);
			}
			return data;
		}

		//===============================================================================\\

		public void CleanUpTickTracker(IEnumerable<int> activeIDs)
		{
			if (activeIDs == null || !activeIDs.Any())
				return;

			List<int> ids = new HashSet<int>(activeIDs).ToList<int>();
			ids.Sort();

			// out of sync errors sometimes throw, unity forums
			// say to make a new list from the keys, instead
			// of using dict.keys in the foreach loop
			List<int> keys = new List<int>(this.TickTracker.Keys);
			foreach (int key in keys)
			{
				if (ids.BinarySearch(key) < 0 && this.TickTracker.ContainsKey(key))
				{
					this.TickTracker.Remove(key);
				}
			}
		}

		//===============================================================================\\

		public float DetermineQualityModifier()
		{
			QualityCategory quality = this.CmpQuality.Quality;
			switch (quality)
			{
				case QualityCategory.Legendary:
					return 1.5f;
				case QualityCategory.Masterwork:
					return 1.1f;
				case QualityCategory.Excellent:
					return 1.0f;
				case QualityCategory.Good:
					return 0.98f;
				case QualityCategory.Normal:
					return 0.97f;
				case QualityCategory.Poor:
					return 0.85f;
				case QualityCategory.Awful:
				default:
					return 0.80f;
			}
		}

		//===============================================================================\\

		public void Persist()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				if (this.TickTracker != null && this.TickTracker.Keys.Count > 0)
				{
					int keys = this.TickTracker.Keys.Count;
					Scribe_Values.Look<int>(ref keys, "KeysCount");

					List<KeyValuePair<int, TickData>> values = this.TickTracker
						.Select(x => x)
						.ToList();
					for (int i = 0; i < values.Count; i++)
					{
						string label = "n" + i.ToString() + "_";
						KeyValuePair<int, TickData> kvp = values[i];
						int keyRef = kvp.Key;

						Scribe_Values.Look<int>(ref keyRef, label + "Key");
						Scribe_Values.Look<float>(ref kvp.Value.Accumulated, label + "Accumulated");
						Scribe_Values.Look<float>(ref kvp.Value.TickAmount, label + "TickAmount");
					}
				}
			}
			else
			{
				if (!_isLoaded)
				{
					_isLoaded = true;
					int keys = 0;
					Scribe_Values.Look<int>(ref keys, "KeysCount", 0);
					if (keys > 0)
					{
						this.TickTracker = new Dictionary<int, TickData>();
						for (int i = 0; i < keys; i++)
						{
							string label = "n" + i.ToString() + "_";
							int keyRef = 0;
							TickData data = new TickData();

							Scribe_Values.Look<int>(ref keyRef, label + "Key");
							Scribe_Values.Look<float>(ref data.Accumulated, label + "Accumulated");
							Scribe_Values.Look<float>(ref data.TickAmount, label + "TickAmount");

							this.TickTracker.Add(keyRef, data);
						}
					}
				}
			}
		}

		//===============================================================================\\
		//===============================================================================\\
		//===============================================================================\\

		public static float GetIncrementApparel(Apparel apparel, INano nanoObj)
		{
			float hp = IsAdvancedResearchComplete()
				? HP_APPAREL_PER_YEAR
				: (HP_APPAREL_PER_YEAR * LOW_TIER_APPAREL);

			float wearPerDay = 0;
			if (apparel.def != null)
				if (apparel.def.apparel != null)
					wearPerDay = apparel.def.apparel.wearPerDay;

			return nanoObj.RareTicksPerYear() / (hp + (60f * wearPerDay));
		}

		//===============================================================================\\

		public static float GetIncrementWeapon(Thing weapon, INano nanoObj)
		{
			float hp = IsAdvancedResearchComplete()
				? HP_WEAPON_PER_YEAR
				: (HP_WEAPON_PER_YEAR * LOW_TIER_WEAPONS);

			return nanoObj.RareTicksPerYear() / hp;
		}

		//===============================================================================\\

		public static bool IsWeaponResearchComplete()
		{
			if (_researchDefWeapons == null)
			{
				lock (_lock)
				{
					if (_researchDefWeapons == null)
					{
						_researchDefWeapons = DefDatabase<ResearchProjectDef>.AllDefs
							.First(x => x.defName == "Ogre_NanoTech_Weapons");

						if (_researchDefWeapons == null)
							throw new NullReferenceException("Could not find ResearchDef: (Ogre_NanoTech_Weapons)");
					}
				}
			}
			return _researchDefWeapons.IsFinished;
		}

		//===============================================================================\\

		public static bool IsAdvancedResearchComplete()
		{
			if (_researchDefAdvanced == null)
			{
				lock (_lock)
				{
					if (_researchDefAdvanced == null)
					{
						_researchDefAdvanced = DefDatabase<ResearchProjectDef>.AllDefs
							.First(x => x.defName == "Ogre_NanoTech_Advanced");

						if (_researchDefAdvanced == null)
							throw new NullReferenceException("Could not find ResearchDef: (Ogre_NanoTech_Advanced)");
					}
				}
			}
			return _researchDefAdvanced.IsFinished;
		}
	}
}
