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
		private readonly static List<SupportedBed> _BEDS_TO_SUPPORT = new List<SupportedBed>()
		{
			// Ideology
			new SupportedBed("Ideology", "SlabBed"),
			new SupportedBed("Ideology", "SlabDoubleBed"),

			// RIMkia
			new SupportedBed("RIMkia", "KRUDNEPPSingle"),
			new SupportedBed("RIMkia", "SLABNEPPDouble"),
			new SupportedBed("RIMkia", "SNOREGGSingle"),
			new SupportedBed("RIMkia", "SNOREGGDouble"),
			new SupportedBed("RIMkia", "PETSNORR"),

			// Gloomy Furniture
			new SupportedBed("Gloomy Furniture", "RGK_bedSingle"),
			new SupportedBed("Gloomy Furniture", "RGK_bedSingleB"),
			new SupportedBed("Gloomy Furniture", "RGK_bedDouble"),
			new SupportedBed("Gloomy Furniture", "RGK_bedDoubleB"),

			// Vanilla Furniture Expanded
			new SupportedBed("Vanilla Furniture Expanded", "Bed_Simple"),
			new SupportedBed("Vanilla Furniture Expanded", "Bed_Ergonomic"),
			new SupportedBed("Vanilla Furniture Expanded", "Bed_DoubleErgonomic"),
			new SupportedBed("Vanilla Furniture Expanded", "Bed_Kingsize"),

			// Vanilla Furniture Expanded Spacer
			new SupportedBed("Vanilla Furniture Expanded - Spacer Module", "Bed_AdvBed"),
			new SupportedBed("Vanilla Furniture Expanded - Spacer Module", "Bed_AdvDoubleBed"),
			// Vanilla Furniture Expanded - Medical Module
			new SupportedBed("Vanilla Furniture Expanded - Medical Module", "Bed_OperatingTable", true)
		};

		public NanoTechMod(ModContentPack content) : base(content)
		{
			Verse.LongEventHandler.QueueLongEvent(() => {
				this.Inject();
			}, "NanoTech_Init", false, null);
		}

		public void Inject()
		{
			Type bed = typeof(Building_Bed);
			Dictionary<string, ThingDef> bedDefs = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(d => !d.defName.StartsWith("Ogre_NanoTech") && bed.IsAssignableFrom(d.thingClass))
				.ToDictionary(x => x.defName, y => y);

			HashSet<string> modSupport = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			List<ThingDef> linkableBuildings = ThingDef.Named("Ogre_NanoTech_Bed").GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities;
			List<CompProperties_Facility> facilities = linkableBuildings
				.Select(x => x.GetCompProperties<CompProperties_Facility>())
				.Where(x => x != null)
				.ToList();

			List<ThingDef> linkableHospitalOnly = ThingDef.Named("Ogre_NanoTech_HospitalBed").GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities;
			List<CompProperties_Facility> hospitalOnlyFacilities = linkableHospitalOnly
				.Select(x => x.GetCompProperties<CompProperties_Facility>())
				.Where(x => x != null)
				.ToList();

			ThingCategoryDef buildingCategory = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Find(x => x.defName == "BuildingsFurniture");

			List<MemeDef> memesRef = new List<MemeDef>();

			Dictionary<string, List<MemeDef>> designatorViaMeme = new Dictionary<string, List<MemeDef>>();
			Dictionary<string, List<RoomRequirement_ThingAnyOf>> reqViaTitle = new Dictionary<string, List<RoomRequirement_ThingAnyOf>>();

			if (ModsConfig.IdeologyActive)
			{
				// build a lookup of all memes
				// that have unlockable buildables
				// ex pain in virtue ( slab beds )
				List<MemeDef> memes = new List<MemeDef>(DefDatabase<MemeDef>.AllDefsListForReading);
				foreach (MemeDef m in memes)
				{
					if (m.addDesignators != null)
					{
						foreach (BuildableDef d in m.addDesignators)
						{
							if (d != null && !string.IsNullOrWhiteSpace(d.defName))
							{
								List<MemeDef> o;
								if (!designatorViaMeme.TryGetValue(d.defName, out o))
								{
									o = new List<MemeDef>();
									designatorViaMeme.Add(d.defName, o);
								}
								o.Add(m);
							}
						}
					}
				}
			}

			if (ModsConfig.RoyaltyActive)
			{
				List<RoyalTitleDef> titles = DefDatabase<RoyalTitleDef>.AllDefsListForReading;
				if (titles != null)
				{
					foreach (RoyalTitleDef title in titles)
					{
						List<RoomRequirement> requirements = title.bedroomRequirements;
						if (requirements != null)
						{
							foreach (RoomRequirement req in requirements)
							{
								RoomRequirement_ThingAnyOf anyReq = (req as RoomRequirement_ThingAnyOf);
								if (anyReq != null)
								{
									List<ThingDef> things = anyReq.things;
									if (things != null && things.Count > 0)
									{
										List<RoomRequirement_ThingAnyOf> o;
										foreach(ThingDef d in things)
										{
											if(!reqViaTitle.TryGetValue(d.defName, out o))
											{
												o = new List<RoomRequirement_ThingAnyOf>();
												reqViaTitle.Add(d.defName, o);
											}
											o.Add(anyReq);
										}
									}	
								}
							}
						}
					}
				}

				modSupport.Add("Royalty");

				Dictionary<string, ThingDef> defaultSupport = new Dictionary<string, ThingDef>() {
					{ "DoubleBed", DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.defName == "Ogre_NanoTech_DoubleBed").First() },
					{ "RoyalBed",  DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.defName == "Ogre_NanoTech_RoyalBed").First() }
				};

				foreach (string key in reqViaTitle.Keys)
				{
					if (defaultSupport.ContainsKey(key))
					{
						foreach (RoomRequirement_ThingAnyOf a in reqViaTitle[key])
						{
							a.things.Add(defaultSupport[key]);
						}
					}
				}
			}

			foreach (SupportedBed b in _BEDS_TO_SUPPORT)
			{
				if (bedDefs.ContainsKey(b.DefName))
				{
					ThingDef nanoBed = NanoUtil.CreateNanoBedDefFromSupportedBed(
						bed: bedDefs[b.DefName],
						fnAdditionalProcessing: b.FnPostProcess,
						linkableBuildings: b.UseHospitalLinkablesOnly ? linkableHospitalOnly : linkableBuildings,
						facilities: b.UseHospitalLinkablesOnly ? hospitalOnlyFacilities : facilities
					);

					DefDatabase<ThingDef>.Add(nanoBed);
					buildingCategory.childThingDefs.Add(nanoBed); // so beds are in stockpile filters
					modSupport.Add(b.ModName);

					if (ModsConfig.IdeologyActive)
					{
						// check if this bed is listed as an 
						// unlockable building via a memedef
						if (!nanoBed.canGenerateDefaultDesignator)
						{
							if (designatorViaMeme.ContainsKey(b.DefName))
							{
								foreach (MemeDef m in designatorViaMeme[b.DefName])
								{
									m.addDesignators.Add(nanoBed);
								}
							}
						}
					}

					if (ModsConfig.RoyaltyActive)
					{
						if (reqViaTitle.ContainsKey(b.DefName))
						{
							foreach (RoomRequirement_ThingAnyOf a in reqViaTitle[b.DefName])
							{
								a.things.Add(nanoBed);
							}
						}
					}
				}
			}

			Verse.Log.Message("Nano Repair Tech Added Support: [ " + string.Join(", ", modSupport.OrderBy(x => x).ToArray()) + " ]");

			// defs show up where they are 
			// supposed to in the game menus?
			DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Find(x => x.defName == "Ogre_NanoRepairTech_DesignationCategory").ResolveReferences();

			// pawns will not auto seek out
			// the beds unless the 
			// RestUtilty is reset
			RestUtility.Reset();
		}
	}
}
