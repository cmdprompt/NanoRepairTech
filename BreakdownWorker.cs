using System;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class BreakdownWorker : RecipeWorker
	{
		public BreakdownWorker() { }

		internal static float GetTechScaler(Thing ingredient)
		{
			if (ingredient == null || ingredient.def == null)
				return 0;

			switch (ingredient.def.techLevel)
			{
				case RimWorld.TechLevel.Undefined:
				case RimWorld.TechLevel.Animal:
				case RimWorld.TechLevel.Neolithic:
					return 0.06f;
				case RimWorld.TechLevel.Medieval:
					return 0.08f;
				case RimWorld.TechLevel.Industrial:
					return 0.1f;
				case RimWorld.TechLevel.Spacer:
				case RimWorld.TechLevel.Ultra:
				case RimWorld.TechLevel.Archotech:
					return 0.17f;
				default:
					return 0;
			}
		}

		public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
		{
			ThingDef breakdown = Verse.DefDatabase<ThingDef>.GetNamed("Ogre_NanoTechFuelBase");
			Thing result = Verse.ThingMaker.MakeThing(breakdown, null);
			float scale = GetTechScaler(ingredient);
			result.stackCount = Math.Max(1, (int)Math.Floor(scale * ingredient.HitPoints));

			Verse.GenPlace.TryPlaceThing(result, ingredient.Position, map, ThingPlaceMode.Near);
			base.ConsumeIngredient(ingredient, recipe, map);
		}

	}
}
