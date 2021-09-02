using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Ogre.NanoRepairTech
{
	public class Hediff_StagesByPercent : HediffWithComps
	{
		public float startingSeverity = 0.5f;

		// orange?
		//private static readonly UnityEngine.Color NanoColor = new UnityEngine.Color(0.89f, 0.38f, 0.07f);

		// blue?
		private static readonly UnityEngine.Color NanoColor = new UnityEngine.Color(0.18f, 0.77f, 1.0f);
		public override UnityEngine.Color LabelColor
		{
			get
			{
				return NanoColor;
			}
		}


		public override int CurStageIndex
		{
			get
			{
				//Verse.Log.Message("InitialSeverity: " + this.startingSeverity);
				if (this.def.stages == null)
				{
					return 0;
				}

				List<HediffStage> stages = this.def.stages;
				float stagePercent = this.Severity / this.startingSeverity;
				//Verse.Log.Message("Stage Percent: " + (this.Severity / this.startingSeverity));
				for (int i = stages.Count - 1; i >= 0; i--)
				{
					if (stagePercent >= stages[i].minSeverity)
					{
						return i;
					}
				}

				return 0;
			}
		}

		public override void ExposeData()
		{
			Scribe_Values.Look<float>(ref this.startingSeverity, "startingSeverity", 1.0f, true);
			base.ExposeData();
		}
	}
}
/*
 // Verse.Hediff
public virtual int CurStageIndex
{
	get
	{
		if (this.def.stages == null)
		{
			return 0;
		}
		List<HediffStage> stages = this.def.stages;
		float severity = this.Severity;
		for (int i = stages.Count - 1; i >= 0; i--)
		{
			if (severity >= stages[i].minSeverity)
			{
				return i;
			}
		}
		return 0;
	}
}

	 */
