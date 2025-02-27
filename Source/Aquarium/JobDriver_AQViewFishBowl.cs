﻿using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Aquarium
{
	// Token: 0x0200000D RID: 13
	public class JobDriver_AQViewFishBowl : JobDriver_VisitJoyThing
	{
		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00004218 File Offset: 0x00002418
		private Thing FishyThing
		{
			get
			{
				return job.GetTarget(TargetIndex.A).Thing;
			}
		}

		// Token: 0x06000056 RID: 86 RVA: 0x0000423C File Offset: 0x0000243C
		protected override void WaitTickAction()
		{
			float num = AQUtility.GetJoyGainFactor(FishyThing.GetStatValue(StatDefOf.Beauty, true) / FishyThing.def.GetStatValueAbstract(StatDefOf.Beauty, null), FishyThing);
			float extraJoyGainFactor = (num > 0f) ? num : 0f;
			pawn.GainComfortFromCellIfPossible(false);
			JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor, (Building)FishyThing);
			AQUtility.ApplyMoodBoostAndInspire(pawn, FishyThing);
		}
	}
}
