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
		//private readonly static string[] _DEFS_TO_SUPPORT = new string[]
		//{
		//	// RIMkia
		//	"KRUDNEPPSingle",
		//	"SLABNEPPDouble",
		//	"SNOREGGSingle",
		//	"SNOREGGDouble"
		//};

		private readonly static List<KeyValuePair<string, Action<ThingDef>>> _BEDS_TO_SUPPORT = new List<KeyValuePair<string, Action<ThingDef>>>()
		{
			// RIMkia
			{ Create("KRUDNEPPSingle", null) },
			{ Create("SLABNEPPDouble", null) },
			{ Create("SNOREGGSingle", null) },
			{ Create("SNOREGGDouble", null) },
			{ Create("PETSNORR", null) },

			// Gloomy Furniture
			{ Create("RGK_bedSingle", null) },
			{ Create("RGK_bedSingleB", null) },
			{ Create("RGK_bedDouble", null) },
			{ Create("RGK_bedDoubleB", null) }
		};

		internal static KeyValuePair<string, Action<ThingDef>> Create(string defName, Action<ThingDef> fnProcess)
		{
			return new KeyValuePair<string, Action<ThingDef>>(defName, fnProcess);
		}

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

			List<string> logDefs = new List<string>();

			List<ThingDef> linkableBuildings = ThingDef.Named("Ogre_NanoTech_Bed").GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities;
			List<CompProperties_Facility> facilities = linkableBuildings
				.Select(x => x.GetCompProperties<CompProperties_Facility>())
				.Where(x => x != null)
				.ToList();

			ThingCategoryDef buildingCategory = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Find(x => x.defName == "BuildingsFurniture");

			foreach (KeyValuePair<string, Action<ThingDef>> kvp in _BEDS_TO_SUPPORT)
			{
				if (bedDefs.ContainsKey(kvp.Key))
				{
					ThingDef nanoBed = NanoUtil.CreateNanoBedDefFromSupportedBed(bedDefs[kvp.Key], kvp.Value, linkableBuildings, facilities);
					DefDatabase<ThingDef>.Add(nanoBed);
					buildingCategory.childThingDefs.Add(nanoBed); // so beds are in stockpiles filters
					logDefs.Add(kvp.Key);
				}
			}

			Verse.Log.Message("Nano Repair Tech Added Defs: { " + string.Join(", ", logDefs.ToArray()) + " }");

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
