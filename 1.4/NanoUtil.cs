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
		internal static ThingDef CreateNanoBedDefFromSupportedBed(this ThingDef bed, List<ThingDef> linkableBuildings, List<CompProperties_Facility> facilities)
		{
			Type typeRimworldBed = typeof(Building_Bed);
			Type bedToClone = bed.GetType();
			if (typeRimworldBed.IsAssignableFrom(bedToClone))
				throw new Exception("Type [" + bedToClone.Name + "] is not supported.");

			//FieldInfo[] fieldsThingDef = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.Instance);

			//ThingDef nBed = new ThingDef();
			//foreach (FieldInfo field in fieldsThingDef)
			//	field.SetValue(nBed, field.GetValue(bed));

			//nBed.comps = new List<CompProperties>();
			//for (int i = 0; i < bed.comps.Count; i++)
			//{
			//	ConstructorInfo constructor = bed.comps[i].GetType().GetConstructor(Type.EmptyTypes);
			//	CompProperties comp = (CompProperties)constructor.Invoke(null);

			//	FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			//	foreach (FieldInfo field in fields)
			//		field.SetValue(comp, field.GetValue(bed.comps[i]));

			//	nBed.comps.Add(comp);
			//}

			ThingDef nBed = InitialCloneWithComps(bed);
			
			nBed.statBases.Add(new StatModifier() { stat = StatDef.Named("Ogre_NanoApparelRate"), value = 0 });
			nBed.statBases.Add(new StatModifier() { stat = StatDef.Named("Ogre_NanoWeaponsRate"), value = 0 });
			
			CompProperties_Power power = new CompProperties_Power();
			power.compClass = typeof(CompPowerTrader);

			//power.basePowerConsumption = 60f;
			// basePowerConsumption is now private
			// which makes me real sad
			FieldInfo fInfo = typeof(CompProperties_Power).GetField("basePowerConsumption", BindingFlags.NonPublic | BindingFlags.Instance);
			fInfo.SetValue(power, 60f);
			
			power.shortCircuitInRain = false;
			nBed.comps.Add(power);

			CompProperties_Flickable flick = new CompProperties_Flickable();
			flick.compClass = typeof(CompFlickable);
			nBed.comps.Add(flick);

			CompProperties_Refuelable fuel = new CompProperties_Refuelable();
			fuel.fuelConsumptionRate = 0;
			fuel.fuelCapacity = 50.0f * bed.size.x; // same way it calculates in BedUtility
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

			nBed.statBases = new List<StatModifier>();
			foreach (StatModifier s in bed.statBases)
			{
				if (s.stat == StatDefOf.WorkToBuild)
				{
					nBed.statBases.Add(new StatModifier()
					{
						stat = StatDefOf.WorkToBuild,
						value = s.value + 2500
					});
				}
				else
				{
					nBed.statBases.Add(s);
				}
			}
			
			ModContentPack nTechModPack = DefDatabase<ThingDef>.GetNamed("Ogre_NanoTech_Bed").modContentPack;
			Type typeThingDef = typeof(ThingDef);

			nBed.modContentPack = nTechModPack;

			// as of 1.3 without this, it wont
			// show the out of fuel icon
			nBed.drawerType = DrawerType.RealtimeOnly;

			nBed.designationCategory = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Find(x => x.defName == "Ogre_NanoRepairTech_DesignationCategory");

			//typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { nBed, typeof(ThingDef) });
			fInfo = typeof(ShortHashGiver).GetField("takenHashesPerDeftype", BindingFlags.NonPublic | BindingFlags.Static);
			Dictionary<Type, HashSet<ushort>> takenHashes = (Dictionary<Type, HashSet<ushort>>)fInfo.GetValue(null);
			
			MethodInfo hashGiver = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static);
			hashGiver.Invoke(null, new object[] { nBed, typeThingDef, takenHashes[typeThingDef] });
			
			// These things do not appear to work as good as
			// they did in 1.2, comment them out and do the
			// frame/blueprint defs by clone

			//MethodInfo newBluePrintDef = typeof(RimWorld.ThingDefGenerator_Buildings).GetMethod("NewBlueprintDef_Thing", BindingFlags.Static | BindingFlags.NonPublic);
			//nBed.blueprintDef = (ThingDef)newBluePrintDef.Invoke(null, new object[] { nBed, false, null });
			//nBed.installBlueprintDef = (ThingDef)newBluePrintDef.Invoke(null, new object[] { nBed, true, null });

			//MethodInfo newFrameDef = typeof(RimWorld.ThingDefGenerator_Buildings).GetMethod("NewFrameDef_Thing", BindingFlags.Static | BindingFlags.NonPublic);
			//nBed.frameDef = (ThingDef)newFrameDef.Invoke(null, new object[] { nBed });

			//ThingDef bluePrint = new ThingDef();
			//foreach (FieldInfo field in fieldsThingDef)
			//	field.SetValue(bluePrint, field.GetValue(bed.blueprintDef));

			//bluePrint.comps = new List<CompProperties>();
			//for (int i = 0; i < bed.blueprintDef.comps.Count; i++)
			//{
			//	ConstructorInfo constructor = bed.blueprintDef.comps[i].GetType().GetConstructor(Type.EmptyTypes);
			//	CompProperties comp = (CompProperties)constructor.Invoke(null);

			//	FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			//	foreach (FieldInfo field in fields)
			//		field.SetValue(comp, field.GetValue(bed.blueprintDef.comps[i]));

			//	bluePrint.comps.Add(comp);
			//}
			
			ThingDef bluePrint = InitialCloneWithComps(bed.blueprintDef);
			
			bluePrint.modContentPack = nTechModPack;
			bluePrint.entityDefToBuild = nBed;
			bluePrint.defName = nBed.defName + "_BluePrint";

			bluePrint.shortHash = 0;
			hashGiver.Invoke(null, new object[] { bluePrint, typeThingDef, takenHashes[typeThingDef] });

			nBed.blueprintDef = bluePrint;
			
			//ThingDef installBluePrint = new ThingDef();
			//foreach (FieldInfo field in fieldsThingDef)
			//	field.SetValue(installBluePrint, field.GetValue(bed.installBlueprintDef));

			//installBluePrint.comps = new List<CompProperties>();
			//for (int i = 0; i < bed.installBlueprintDef.comps.Count; i++)
			//{
			//	ConstructorInfo constructor = bed.installBlueprintDef.comps[i].GetType().GetConstructor(Type.EmptyTypes);
			//	CompProperties comp = (CompProperties)constructor.Invoke(null);

			//	FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			//	foreach (FieldInfo field in fields)
			//		field.SetValue(comp, field.GetValue(bed.installBlueprintDef.comps[i]));

			//	installBluePrint.comps.Add(comp);
			//}
			
			ThingDef installBluePrint = InitialCloneWithComps(bed.installBlueprintDef);
			
			installBluePrint.modContentPack = nTechModPack;
			installBluePrint.entityDefToBuild = nBed;
			installBluePrint.defName = nBed.defName + "_InstallBluePrint";

			
			installBluePrint.shortHash = 0;
			hashGiver.Invoke(null, new object[] { installBluePrint, typeThingDef, takenHashes[typeThingDef] });
			
			nBed.installBlueprintDef = installBluePrint;

			//ThingDef frameDef = new ThingDef();
			//foreach (FieldInfo field in fieldsThingDef)
			//	field.SetValue(frameDef, field.GetValue(bed.frameDef));

			//frameDef.comps = new List<CompProperties>();

			//for (int i = 0; i < bed.frameDef.comps.Count; i++)
			//{
			//	ConstructorInfo constructor = bed.frameDef.comps[i].GetType().GetConstructor(Type.EmptyTypes);
			//	CompProperties comp = (CompProperties)constructor.Invoke(null);

			//	FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			//	foreach (FieldInfo field in fields)
			//		field.SetValue(comp, field.GetValue(bed.frameDef.comps[i]));

			//	frameDef.comps.Add(comp);
			//}
			
			ThingDef frameDef = InitialCloneWithComps(bed.frameDef);
			
			frameDef.modContentPack = nTechModPack;
			frameDef.entityDefToBuild = nBed;
			frameDef.defName = nBed.defName + "_Frame";

			
			frameDef.shortHash = 0;
			hashGiver.Invoke(null, new object[] { frameDef, typeThingDef, takenHashes[typeThingDef] });
			
			nBed.frameDef = frameDef;

			// Breaks on save if these defs
			// are not loaded into the DefDatabase
			DefDatabase<ThingDef>.Add(bluePrint);
			DefDatabase<ThingDef>.Add(installBluePrint);
			DefDatabase<ThingDef>.Add(frameDef);

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


			return nBed;
		}

		internal static ThingDef InitialCloneWithComps(ThingDef thing)
		{
			FieldInfo[] fieldsThingDef = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.Instance);
			ThingDef rv = new ThingDef();

			foreach (FieldInfo field in fieldsThingDef)
				field.SetValue(rv, field.GetValue(thing));

			rv.comps = new List<CompProperties>();
			for (int i = 0; i < thing.comps.Count; i++)
			{
				ConstructorInfo constructor = thing.comps[i].GetType().GetConstructor(Type.EmptyTypes);
				CompProperties comp = (CompProperties)constructor.Invoke(null);

				FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo field in fields)
					field.SetValue(comp, field.GetValue(thing.comps[i]));

				rv.comps.Add(comp);
			}

			return rv;
		}
	}
}
