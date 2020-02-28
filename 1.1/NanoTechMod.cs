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
