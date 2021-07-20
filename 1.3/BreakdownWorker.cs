using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class BreakdownWorker : RecipeWorker
	{
		public BreakdownWorker() { }

		//===============================================================================\\

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

		//===============================================================================\\

		public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
		{
			ThingDef breakDown = Verse.DefDatabase<ThingDef>.GetNamed("Ogre_NanoTechFuelBase");

			int stackCount = 0;

			for (int i = ingredients.Count - 1; i > -1; i--)
			{
				float scale = GetTechScaler(ingredients[i]);
				stackCount += Math.Max(1, (int)Math.Floor(scale * ingredients[i].HitPoints));
			}

			// <products> count=0 breaks the bills
			// if action is drop on floor, to fix it
			// let the count = 1 be the default
			// and subtract 1 from the stackcount
			// for all other cases

			if (stackCount == 1)
			{
				// let default recipe def
				// generate the fuel item
			}
			else
			{
				// add stackCount -1 to what
				// the fuel base total they are already carrying
				
				Thing result = Verse.ThingMaker.MakeThing(breakDown, null);
				result.stackCount = stackCount -1;
				billDoer.carryTracker.TryStartCarry(result);
			}

			base.Notify_IterationCompleted(billDoer, ingredients);
		}
	}
}
