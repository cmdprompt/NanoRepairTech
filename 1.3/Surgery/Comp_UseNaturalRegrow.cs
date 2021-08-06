using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class CompUseEffect_NanoTechNaturalBodyPartGrower : CompUseEffect
	{
		public override void DoEffect(Pawn usedBy)
		{
			List<Hediff_MissingPart> parts = new List<Hediff_MissingPart>();
			HashSet<BodyPartRecord> missingOnPurpose = new HashSet<BodyPartRecord>();

			for (int i = 0; i < usedBy.health.hediffSet.hediffs.Count; i++)
			{
				Hediff_MissingPart p = (usedBy.health.hediffSet.hediffs[i] as Hediff_MissingPart);
				if (p != null)
				{
					parts.Add(p);
					continue;
				}

				Hediff_AddedPart a = (usedBy.health.hediffSet.hediffs[i] as Hediff_AddedPart);
				if (a != null)
					missingOnPurpose.Add(a.Part);
				
			}

			if (parts.Count == 0)
			{
				Verse.Log.Message("No Missing Parts Found.");
			}

			if (parts.Count > 0)
			{
				for (int i = parts.Count - 1; i > -1; i--)
				{
					for (BodyPartRecord r = parts[i].Part; r != null; r = r.parent)
					{
						if (missingOnPurpose.Contains(r))
						{
							Verse.Log.Message("Missing on Purpose. (" + parts[i].Part.customLabel + ")(" + r.customLabel + ")");
							parts.RemoveAt(i);
							break;
						}
					}
				}

				if (parts.Count == 0)
				{
					Verse.Log.Message("No elligible parts left.");
				}
				else
				{
					// find closest parent that is missing.
					BodyPartRecord record = parts[0].Part;
					for (BodyPartRecord r = record.parent; r != null; r = r.parent)
					{
						if (usedBy.health.hediffSet.PartIsMissing(r))
						{
							record = r;
						}
					}
					Verse.Log.Message("Part 0 (" + parts[0].Part.customLabel + ") Parent to Replace (" + record.customLabel + ")");
					usedBy.health.RestorePart(record, null, true);

					float totalHitpoints = 0;
					Stack<BodyPartRecord> stack = new Stack<BodyPartRecord>();
					stack.Push(record);
					while (stack.Count > 0)
					{
						BodyPartRecord r = stack.Pop();
						if (r.def.hitPoints <= 10f)
						{
							Verse.Log.Message(" => [10] (" + r.customLabel + ")");
							totalHitpoints += 10f;
						}
						else
						{
							Verse.Log.Message(" => [" + r.def.hitPoints + "] (" + r.customLabel + ")");
							totalHitpoints += r.def.hitPoints;
						}
						if (r.parts != null && r.parts.Count > 0)
						{
							for (int i = 0; i < r.parts.Count; i++)
							{
								stack.Push(r.parts[i]);
							}
						}
					}

					Verse.Log.Message(" TotalHP: " + totalHitpoints);
				}
			}
			
			
			base.DoEffect(usedBy);
		}
	}
}
