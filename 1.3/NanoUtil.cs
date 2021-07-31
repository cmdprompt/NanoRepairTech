using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using System.Reflection;
using System.Collections;

namespace Ogre.NanoRepairTech
{
	internal static class NanoUtil
	{
		internal static ThingDef CreateNanoBedDefFromSupportedBed(this ThingDef bed, Action<ThingDef> fnAdditionalProcessing, List<ThingDef> linkableBuildings, List<CompProperties_Facility> facilities)
		{
			Type typeRimworldBed = typeof(Building_Bed);
			Type bedToClone = bed.GetType();
			if (typeRimworldBed.IsAssignableFrom(bedToClone))
				throw new Exception("Type [" + bedToClone.Name + "] is not supported.");

			FieldInfo[] fields = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.Instance);
			ThingDef nBed = new ThingDef();
			foreach (FieldInfo field in fields)
				field.SetValue(nBed, field.GetValue(bed));

			nBed.comps = new List<CompProperties>();
			for (int i = 0; i < bed.comps.Count; i++)
			{
				ConstructorInfo constructor = bed.comps[i].GetType().GetConstructor(Type.EmptyTypes);
				CompProperties comp = (CompProperties)constructor.Invoke(null);

				fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo field in fields)
					field.SetValue(comp, field.GetValue(bed.comps[i]));

				nBed.comps.Add(comp);
			}

			nBed.statBases.Add(new StatModifier() { stat = StatDef.Named("Ogre_NanoApparelRate"), value = 0 });
			nBed.statBases.Add(new StatModifier() { stat = StatDef.Named("Ogre_NanoWeaponsRate"), value = 0 });

			CompProperties_Power power = new CompProperties_Power();
			power.compClass = typeof(CompPowerTrader);
			power.basePowerConsumption = 60f;
			power.shortCircuitInRain = false;
			nBed.comps.Add(power);

			CompProperties_Flickable flick = new CompProperties_Flickable();
			flick.compClass = typeof(CompFlickable);
			nBed.comps.Add(flick);

			CompProperties_Refuelable fuel = new CompProperties_Refuelable();
			fuel.fuelConsumptionRate = 0;
			fuel.fuelCapacity = 25.0f * bed.size.x; // same way it calculates in BedUtility
			fuel.consumeFuelOnlyWhenUsed = true;
			fuel.fuelFilter = new ThingFilter();
			fuel.fuelFilter.SetAllow(ThingDef.Named("Ogre_NanoTechFuel"), true);
			nBed.comps.Add(fuel);
			
			Dictionary<string, int> cost = new Dictionary<string, int>()
			{
				{ "ComponentIndustrial", 1 },
				{ "Steel", 5 }
			};

			if (nBed.costList == null)
				nBed.costList = new List<ThingDefCountClass>();

			Dictionary<string, ThingDefCountClass> current = nBed.costList.ToDictionary(x => x.thingDef.defName, y => y);


			foreach (string item in cost.Keys)
			{
				ThingDefCountClass count = null;
				if (!current.TryGetValue(item, out count))
				{
					count = new ThingDefCountClass(ThingDef.Named(item), (cost[item] * nBed.size.x));
					nBed.costList.Add(count);
				}
				else
				{
					count.count += (cost[item] * nBed.size.x);
				}
			}

			bool found = false;
			nBed.researchPrerequisites = new List<ResearchProjectDef>();
			if (bed.researchPrerequisites != null && bed.researchPrerequisites.Count > 0)
			{
				foreach (ResearchProjectDef d in bed.researchPrerequisites)
				{
					if (d.defName == "Ogre_NanoTech") { found = true; }
					nBed.researchPrerequisites.Add(d);
				}
			}

			if (!found)
				nBed.researchPrerequisites.Add(ResearchProjectDef.Named("Ogre_NanoTech"));


			nBed.defName += "_NanoBed";
			nBed.description += "\n\n" + TranslatorFormattedStringExtensions.Translate("NanoTech.Description.Short");
			nBed.label = TranslatorFormattedStringExtensions.Translate("NanoTech.ModName.Short") + " " + nBed.label;
			nBed.thingClass = typeof(NanoBed);
			nBed.tradeability = Tradeability.None;
			nBed.scatterableOnMapGen = false;
			nBed.tickerType = TickerType.Rare;
			nBed.constructionSkillPrerequisite = bed.constructionSkillPrerequisite < 2 ? 2 : bed.constructionSkillPrerequisite;
			nBed.uiIconScale = 0.9f;
			nBed.techLevel = TechLevel.Industrial;
			nBed.shortHash = 0;

			// as of 1.3 without this, it wont
			// show the out of fuel icon
			nBed.drawerType = DrawerType.RealtimeOnly;

			nBed.designationCategory = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Find(x => x.defName == "Ogre_NanoRepairTech_DesignationCategory");

			MethodInfo newBluePrintDef = typeof(RimWorld.ThingDefGenerator_Buildings).GetMethod("NewBlueprintDef_Thing", BindingFlags.Static | BindingFlags.NonPublic);
			nBed.blueprintDef = (ThingDef)newBluePrintDef.Invoke(null, new object[] { nBed, false, null });

			MethodInfo newFrameDef = typeof(RimWorld.ThingDefGenerator_Buildings).GetMethod("NewFrameDef_Thing", BindingFlags.Static | BindingFlags.NonPublic);
			nBed.frameDef = (ThingDef)newFrameDef.Invoke(null, new object[] { nBed });

			if (bed.building.bed_humanlike)
			{
				CompProperties_AffectedByFacilities abf = nBed.GetCompProperties<CompProperties_AffectedByFacilities>();
				if (abf == null)
				{
					abf = new CompProperties_AffectedByFacilities();
					nBed.comps.Add(abf);
				}

				if (abf.linkableFacilities == null)
					abf.linkableFacilities = new List<ThingDef>();

				abf.linkableFacilities.AddRange(linkableBuildings);

				foreach (CompProperties_Facility f in facilities)
					f.linkableBuildings.Add(nBed);
			}

			if (fnAdditionalProcessing != null)
				fnAdditionalProcessing.Invoke(nBed);

			typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { nBed, typeof(ThingDef) });

			return nBed;
		}
	}
}
