using System;
using System.Collections.Generic;
using Verse;
using System.Linq;
using RimWorld;
using System.Reflection;

namespace Ogre.NanoRepairTech
{
	public class NanoTechMod : Verse.Mod
	{
		private readonly static string[] _DEFS_TO_SUPPORT = new string[]
		{
			// RIMkia
			"KRUDNEPPSingle"
		};

		public NanoTechMod(ModContentPack content) : base(content)
		{
			Verse.LongEventHandler.QueueLongEvent(() =>
			{
				this.Inject();
			}, "NanoTech_Init", false, null);
		}

		public void Inject()
		{
			Type bed = typeof(Building_Bed);
			Dictionary<string, ThingDef> bedDefs = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(d => !d.defName.StartsWith("Ogre_NanoTech") && bed.IsAssignableFrom(d.thingClass))
				.ToDictionary(x => x.defName, y => y);

			FieldInfo[] fields = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.Instance);
			StatDef defApparelRate = StatDef.Named("Ogre_NanoApparelRate");
			StatDef defWeaponRate = StatDef.Named("Ogre_NanoWeaponsRate");


			DesignationCategoryDef catDef = DefDatabase<DesignationCategoryDef>.AllDefs.First(x => x.defName == "Ogre_NanoRepairTech_DesignationCategory");
			List<string> logDefs = new List<string>();
			List<ThingDef> linkableBuildings = ThingDef.Named("Ogre_NanoTech_Bed").GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities;

			foreach (string defToSupport in _DEFS_TO_SUPPORT)
			{
				if (bedDefs.ContainsKey(defToSupport))
				{
					ThingDef nanoBed = new ThingDef();
					ThingDef bedToSupport = bedDefs[defToSupport];
					float sleepSlots = Convert.ToSingle(BedUtility.GetSleepingSlotsCount(bedToSupport.Size));

					foreach (FieldInfo info in fields)
						info.SetValue(nanoBed, info.GetValue(bedToSupport));

					nanoBed.defName = "Ogre_NanoTech_" + bedToSupport.defName;
					nanoBed.label = TranslatorFormattedStringExtensions.Translate("NanoTech.ModName.Short") + " " + nanoBed.label;
					nanoBed.description += " " + TranslatorFormattedStringExtensions.Translate("NanoTech.Description.Short");
					nanoBed.thingClass = typeof(NanoBed);
					nanoBed.shortHash = 0;
					//nanoBed.minifiedDef = ThingDefOf.MinifiedThing;
					nanoBed.tradeability = Tradeability.None;
					nanoBed.scatterableOnMapGen = false;

					// Stat Bases
					nanoBed.statBases.Add(new StatModifier()
					{
						stat = defApparelRate,
						value = 0
					});
					nanoBed.statBases.Add(new StatModifier()
					{
						stat = defWeaponRate,
						value = 0
					});

					// Copy Comps
					copyComps(nanoBed, bedToSupport);

					// POWER
					CompProperties_Power power = nanoBed.GetCompProperties<CompProperties_Power>();
					if (power == null)
					{
						power = new CompProperties_Power();
						nanoBed.comps.Add(power);
					}

					power.compClass = typeof(CompPowerTrader);
					power.basePowerConsumption = 60;
					power.shortCircuitInRain = false;

					// FLICK
					CompProperties_Flickable flickable = nanoBed.GetCompProperties<CompProperties_Flickable>();
					if (flickable == null)
					{
						flickable = new CompProperties_Flickable();
						nanoBed.comps.Add(flickable);
					}
					flickable.compClass = typeof(CompFlickable);

					// FUEL
					CompProperties_Refuelable fuel = nanoBed.GetCompProperties<CompProperties_Refuelable>();
					if (fuel == null)
					{
						fuel = new CompProperties_Refuelable();
						nanoBed.comps.Add(fuel);
					}

					fuel.compClass = typeof(CompRefuelable);
					fuel.fuelConsumptionRate = 0;
					fuel.fuelCapacity = 25.0f * sleepSlots;
					fuel.consumeFuelOnlyWhenUsed = true;
					ThingFilter filter = new ThingFilter();
					filter.SetAllow(ThingDef.Named("Ogre_NanoTechFuel"), true);
					fuel.fuelFilter = filter;

					// facilities
					if (bedToSupport.building.bed_humanlike)
					{
						CompProperties_AffectedByFacilities abf = nanoBed.GetCompProperties<CompProperties_AffectedByFacilities>();
						if (abf == null)
						{
							abf = new CompProperties_AffectedByFacilities();
							nanoBed.comps.Add(abf);
						}

						abf.compClass = typeof(CompAffectedByFacilities);

						if (abf.linkableFacilities == null)
							abf.linkableFacilities = new List<ThingDef>();

						foreach (ThingDef facility in linkableBuildings)
						{
							HashSet<string> highlander = new HashSet<string>(abf.linkableFacilities.Select(x => x.defName));
							if (!highlander.Contains(facility.defName))
							{
								abf.linkableFacilities.Add(facility);
								Verse.Log.Message("Add: " + facility.defName);
							}
							else
							{
								Verse.Log.Message("Already: " + facility.defName);
							}

						}
					}

					// Cost List
					addToCostList(nanoBed, "ComponentIndustrial", 2 * (int)sleepSlots);
					addToCostList(nanoBed, "Steel", 30 * (int)sleepSlots);
					addToCostList(nanoBed, "Plasteel", 15 * (int)sleepSlots);


					// Research
					if (nanoBed.researchPrerequisites == null)
						nanoBed.researchPrerequisites = new List<ResearchProjectDef>();

					nanoBed.researchPrerequisites.Add(ResearchProjectDef.Named("Ogre_NanoTech"));

					// Cleanup
					nanoBed.tickerType = TickerType.Rare;
					nanoBed.techLevel = TechLevel.Industrial;
					nanoBed.constructionSkillPrerequisite = 8;
					nanoBed.designationCategory = catDef;
					
					typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { nanoBed, typeof(ThingDef) });
					DefDatabase<ThingDef>.Add(nanoBed);

					logDefs.Add(nanoBed.defName);
				}
			}

			Verse.Log.Message("Nano Repair Tech Added Defs: { " + string.Join(", ", logDefs.ToArray()) + " }");
		}

		private static void addToCostList(ThingDef def, string defName, int count)
		{
			if (def.costList == null)
				def.costList = new List<ThingDefCountClass>();

			ThingDefCountClass item = def.costList.FirstOrDefault(x => x.thingDef.defName == defName);
			if (item == null)
			{
				item = new ThingDefCountClass(ThingDef.Named(defName), 0);
				def.costList.Add(item);
			}

			item.count += count;
			if (item.count <= 0)
				item.count = 1;
		}

		private static void copyComps(ThingDef nanoBed, ThingDef supportedBed)
		{
			nanoBed.comps = new List<CompProperties>();
			foreach (CompProperties p in supportedBed.comps)
			{
				ConstructorInfo constructor = p.GetType().GetConstructor(Type.EmptyTypes);
				CompProperties clone = (CompProperties)constructor.Invoke(null);

				FieldInfo[] fields = clone.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo info in fields)
					info.SetValue(clone, info.GetValue(p));

				nanoBed.comps.Add(clone);
			}
		}
	}
}
