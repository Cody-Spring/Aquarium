﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Aquarium
{
	// Token: 0x02000006 RID: 6
	public class CompAquarium : ThingComp
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000019 RID: 25 RVA: 0x00002BE2 File Offset: 0x00000DE2
		private CompProperties_CompAquarium Props
		{
			get
			{
				return (CompProperties_CompAquarium)this.props;
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002BF0 File Offset: 0x00000DF0
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.numFish, "numFish", 0, false);
			Scribe_Values.Look<float>(ref this.cleanPct, "cleanPct", 1f, false);
			Scribe_Values.Look<float>(ref this.foodPct, "foodPct", 0f, false);
			Scribe_Collections.Look<string>(ref this.fishData, "fishData", LookMode.Value, Array.Empty<object>());
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002C58 File Offset: 0x00000E58
		public override void PostDraw()
		{
			if (this.parent.Spawned && this.parent.Map == Current.Game.CurrentMap && this.fishData.Count > 0)
			{
				int count = 0;
				foreach (string value in this.fishData)
				{
					if (CompAquarium.numValuePart(value, 4) != 1)
					{
						count++;
						string defname = CompAquarium.stringValuePart(value, 1);
						string FishPath = "Things/Fish/";
						if (Find.TickManager.TicksGame / 500 % 2 == 0)
						{
							FishPath += "Flip/";
						}
						var gfxName = WordsToNumbers(defname.Replace("AQFishInBag", ""));
						FishPath += $"Fish{gfxName}";
						Graphic FishImage = GraphicDatabase.Get<Graphic_Single>(FishPath, ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);
						int age = CompAquarium.numValuePart(value, 3);
						float ageFactor = Mathf.Lerp(0.5f, 1f, Math.Min((float)age, (float)CompAquarium.oldFishAge) / (float)CompAquarium.oldFishAge);
						Vector3 drawPos = this.parent.DrawPos;
						drawPos.z -= 0.1f;
						float perspective = 1f;
						if (this.Props.maxFish > 1)
						{
							switch (count)
							{
							case 1:
								drawPos.x -= 0.4f;
								break;
							case 2:
								drawPos.x += 0.4f;
								break;
							case 3:
								drawPos.x -= 0.4f;
								drawPos.z += 0.5f;
								perspective = 0.85f;
								break;
							case 4:
								drawPos.x += 0.4f;
								drawPos.z += 0.5f;
								perspective = 0.85f;
								break;
							}
						}
						if (!Find.TickManager.Paused)
						{
							drawPos.x += CompAquarium.RandomFloat(-0.025f, 0.025f);
							drawPos.z += CompAquarium.RandomFloat(-0.025f, 0.025f);
						}
						drawPos.y += 0.0454545468f;
						FishImage.drawSize = new Vector2(ageFactor * perspective, ageFactor * perspective);
						FishImage.Draw(drawPos, Rot4.North, this.parent, 0f);
					}
				}
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002ED0 File Offset: 0x000010D0
		public override void CompTick()
		{
			base.CompTick();
			if (this.parent.Spawned)
			{
				if (Controller.Settings.AllowTankSounds && this.Props.powerNeeded && this.IsPowered())
				{
					this.DoTankSound();
				}
				if (this.parent.IsHashIntervalTick(300))
				{
					if (this.Props.powerNeeded && this.IsPowered() && this.ShouldPushHeatNow)
					{
						GenTemperature.PushHeat(this.parent.PositionHeld, this.parent.MapHeld, this.Props.heatPerSecond * 5f);
					}
					float ageFactor = this.GetAvgAgeMultiplier(this.fishData);
					float newFoodPct;
					this.DegradeFood(this.numFish, this.foodPct, ageFactor, out newFoodPct);
					this.foodPct = newFoodPct;
					float newCleanPct;
					this.DegradeWater(this.numFish, this.foodPct, this.cleanPct, ageFactor, out newCleanPct);
					this.cleanPct = newCleanPct;
					int newNumFish;
					List<string> newFishData;
					this.EffectFish(this.numFish, this.fishData, this.foodPct, this.cleanPct, out newNumFish, out newFishData);
					this.numFish = newNumFish;
					this.fishData = newFishData;
					if (this.fishydebug)
					{
						AQUtility.DebugFishData(this, this.Props.maxFish);
					}
				}
				if (this.parent.IsHashIntervalTick(300000))
				{
					this.TankBreeding();
				}
			}
			else
			{
				this.numFish = 0;
				this.DumpFish(this.fishData);
				this.fishData = new List<string>();
				this.foodPct = 0f;
				this.cleanPct = 1f;
			}
			this.numFish = this.fishData.Count;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00003070 File Offset: 0x00001270
		internal float GetAvgAgeMultiplier(List<string> fdata)
		{
			float factor = 1f;
			float sum = 0f;
			int count = 0;
			if (fdata.Count > 0)
			{
				foreach (string value in fdata)
				{
					if (CompAquarium.numValuePart(value, 4) != 1)
					{
						int age = CompAquarium.numValuePart(value, 3);
						sum += Mathf.Lerp(0.75f, 1f, (float)(Math.Min(CompAquarium.oldFishAge, age) / CompAquarium.oldFishAge));
						count++;
					}
				}
			}
			if (count > 0)
			{
				factor = sum / (float)count;
			}
			return factor;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00003118 File Offset: 0x00001318
		public override void CompTickRare()
		{
			base.CompTickRare();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00003120 File Offset: 0x00001320
		public static float RandomFloat(float min, float max)
		{
			return Rand.Range(min, max);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000312C File Offset: 0x0000132C
		private void DumpFish(List<string> fishData)
		{
			if (this.parent.Map != null && this.parent.Map.ParentFaction == Faction.OfPlayerSilentFail && fishData != null && fishData.Count > 0)
			{
				foreach (string value in fishData)
				{
					int health = CompAquarium.numValuePart(value, 2);
					int age = CompAquarium.numValuePart(value, 3);
					Thing newThing = ThingMaker.MakeThing(ThingDef.Named(CompAquarium.stringValuePart(value, 1)), null);
					newThing.stackCount = 1;
					if (newThing != null)
					{
						Thing newFishbag;
						GenPlace.TryPlaceThing(newThing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near, out newFishbag, null, null, default(Rot4));
						if (newFishbag != null)
						{
							newFishbag.stackCount = 1;
							CompAQFishInBag CBag = newFishbag.TryGetComp<CompAQFishInBag>();
							if (CBag != null)
							{
								CBag.age = age;
								CBag.fishhealth = health;
							}
						}
					}
				}
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00003238 File Offset: 0x00001438
		private void DegradeFood(int numFish, float foodPct, float ageF, out float newFoodPct)
		{
			float factor = Controller.Settings.DegradeFoodFactor / 100f * 0.0625f * ageF;
			newFoodPct = foodPct;
			newFoodPct -= (0.01f + (float)numFish * CompAquarium.RandomFloat(0.01f, 0.04f)) * factor;
			if (newFoodPct < 0f)
			{
				newFoodPct = 0f;
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00003298 File Offset: 0x00001498
		private void DegradeWater(int numFish, float foodPct, float cleanPct, float ageF, out float newCleanPct)
		{
			float factor = Controller.Settings.DegradeWaterFactor / 100f * 0.025f * ageF;
			newCleanPct = cleanPct;
			newCleanPct -= (float)numFish * CompAquarium.RandomFloat(0.02f, 0.03f) * factor;
			newCleanPct -= Mathf.Lerp(0f, 0.02f, foodPct) * factor;
			if (newCleanPct < 0f)
			{
				newCleanPct = 0f;
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000330C File Offset: 0x0000150C
		private void EffectFish(int numFish, List<string> fishData, float foodPct, float cleanPct, out int newNumFish, out List<string> newFishData)
		{
			newNumFish = numFish;
			newFishData = fishData;
			if (numFish > 0)
			{
				int degradingHealth = 0;
				if (foodPct <= 0f)
				{
					degradingHealth++;
				}
				if (cleanPct <= 0.25f)
				{
					degradingHealth++;
				}
				if (this.parent.AmbientTemperature > 55f || this.parent.AmbientTemperature < 1f)
				{
					degradingHealth++;
				}
				int newIndex = 0;
				List<string> changedFishData = new List<string>();
				if (newFishData.Count > 0)
				{
					foreach (string value in newFishData)
					{
						bool died = false;
						CompAquarium.numValuePart(value, 0);
						string defval = CompAquarium.stringValuePart(value, 1);
						int health = CompAquarium.numValuePart(value, 2);
						int age = CompAquarium.numValuePart(value, 3);
						int action = CompAquarium.numValuePart(value, 4);
						int agedegradingHealth = 0;
						if (action != 1)
						{
							age += 300;
							if (age + (int)CompAquarium.RandomFloat(-450000f, 450000f) > CompAquarium.oldFishAge)
							{
								agedegradingHealth = 1;
							}
							int poorhealth = degradingHealth + agedegradingHealth;
							if (poorhealth > 0)
							{
								health -= (int)Math.Max(1f, (float)poorhealth * Mathf.Lerp(0.5f, 1f, Math.Max(1f, (float)age / (float)CompAquarium.oldFishAge)));
								if (health <= 0)
								{
									died = true;
								}
							}
							else if (health < 100)
							{
								health += (int)Math.Max(1f, Mathf.Lerp(1f, 0.5f, Math.Max(1f, (float)age / (float)CompAquarium.oldFishAge)));
								if (health > 100)
								{
									health = 100;
								}
							}
						}
						if (!died)
						{
							newIndex++;
							string newValue = CompAquarium.CreateValuePart(newIndex, defval, health, age, action);
							changedFishData.Add(newValue);
						}
						else
						{
							if (this.parent.Spawned)
							{
								ThingWithComps parent = this.parent;
								if (((parent != null) ? parent.Map : null) != null && this.parent.Map.ParentFaction == Faction.OfPlayerSilentFail)
								{
									if (Controller.Settings.DoDeathMsgs)
									{
										Messages.Message("Aquarium.FishDied".Translate(), this.parent, MessageTypeDefOf.NegativeEvent, false);
									}
									AQUtility.DoSpawnTropicalFishMeat(this.parent, age);
								}
							}
							newNumFish--;
							if (newNumFish < 0)
							{
								newNumFish = 0;
							}
						}
					}
				}
				newFishData = changedFishData;
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00003570 File Offset: 0x00001770
		internal static string CreateValuePart(int newIndex, string defVal, int health, int age, int action)
		{
			return string.Concat(new string[]
			{
				newIndex.ToString(),
				";",
				defVal,
				";",
				health.ToString(),
				";",
				age.ToString(),
				";",
				action.ToString()
			});
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000035D8 File Offset: 0x000017D8
		internal static int numValuePart(string value, int pos)
		{
			char[] divider = new char[]
			{
				';'
			};
			string[] segments = value.Split(divider);
			try
			{
				return int.Parse(segments[pos]);
			}
			catch (FormatException)
			{
				Log.Message("Unable to parse Segment: '" + segments[pos] + "' : " + pos.ToString(), false);
			}
			return 0;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x0000363C File Offset: 0x0000183C
		internal static string stringValuePart(string value, int pos)
		{
			char[] divider = new char[]
			{
				';'
			};
			return value.Split(divider)[pos];
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00003660 File Offset: 0x00001860
		public void StartMixSustainer()
		{
			SoundInfo info = SoundInfo.InMap(this.parent, MaintenanceType.PerTick);
			this.mixSustainer = SoundDef.Named("AQFishTank").TrySpawnSustainer(info);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003695 File Offset: 0x00001895
		private void DoTankSound()
		{
			if (this.mixSustainer == null)
			{
				this.StartMixSustainer();
				return;
			}
			if (this.mixSustainer.Ended)
			{
				this.StartMixSustainer();
				return;
			}
			this.mixSustainer.Maintain();
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000036C5 File Offset: 0x000018C5
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			int num;
			for (int i = 1; i <= this.Props.maxFish; i = num + 1)
			{
				ThingDef fishDef = null;
				int fishHealth = 0;
				int fishAge = 0;
				int fishAction = 0;
				string fishstring = null;
				if (this.fishData.Count > 0)
				{
					foreach (string value in this.fishData)
					{
						if (CompAquarium.numValuePart(value, 0) == i)
						{
							fishstring = CompAquarium.stringValuePart(value, 1);
							if (fishstring != null)
							{
								fishDef = ThingDef.Named(fishstring);
							}
							fishHealth = CompAquarium.numValuePart(value, 2);
							fishAge = (int)((float)CompAquarium.numValuePart(value, 3) / 60000f);
							fishAction = CompAquarium.numValuePart(value, 4);
							break;
						}
					}
				}
				string numLabel = i.ToString() + ": ";
				string fishLabel = numLabel + "Aquarium.NoFish".Translate();
				string graphicPath = "Things/Fish/UI/NoFish";
				string fishDesc = "Aquarium.FishSelection".Translate();
				if (fishDef != null)
				{
					var fishGfx = fishstring.Replace("AQFishInBag", "");
					graphicPath = $"Things/Fish/Fish{WordsToNumbers(fishGfx)}";
					if (fishAction == 1)
					{
						fishLabel = numLabel + "Aquarium.Adding".Translate();
					}
					else if (fishAction == 2)
					{
						fishLabel = numLabel + "Aquarium.Removing".Translate();
					}
					else
					{
						fishLabel = numLabel + fishDef.LabelCap + "\nH: " + ((float)fishHealth / 100f).ToStringPercent() + ", " + fishAge.ToString() + " " + "Aquarium.days".Translate();
					}
				}
				Texture2D fishIcon = ContentFinder<Texture2D>.Get(graphicPath, true);
				if (i <= this.Props.maxFish)
				{
					switch (i)
					{
					case 1:
						yield return new Command_Action
						{
							defaultLabel = fishLabel,
							icon = fishIcon,
							defaultDesc = fishDesc,
							action = delegate()
							{
								this.SelectFish(fishDef, 1);
							}
						};
						break;
					case 2:
						yield return new Command_Action
						{
							defaultLabel = fishLabel,
							icon = fishIcon,
							defaultDesc = fishDesc,
							action = delegate()
							{
								this.SelectFish(fishDef, 2);
							}
						};
						break;
					case 3:
						yield return new Command_Action
						{
							defaultLabel = fishLabel,
							icon = fishIcon,
							defaultDesc = fishDesc,
							action = delegate()
							{
								this.SelectFish(fishDef, 3);
							}
						};
						break;
					case 4:
						yield return new Command_Action
						{
							defaultLabel = fishLabel,
							icon = fishIcon,
							defaultDesc = fishDesc,
							action = delegate()
							{
								this.SelectFish(fishDef, 4);
							}
						};
						break;
					}
				}
				num = i;
			}
			if (Prefs.DevMode)
			{
				yield return new Command_Toggle
				{
					icon = ContentFinder<Texture2D>.Get(this.debugTexPath, true),
					defaultLabel = "Debug Mode",
					defaultDesc = "Send debug messages to Log",
					isActive = (() => this.fishydebug),
					toggleAction = delegate()
					{
						this.ToggleDebug(this.fishydebug);
					}
				};
			}
			yield break;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000036D5 File Offset: 0x000018D5
		public void ToggleDebug(bool flag)
		{
			this.fishydebug = !flag;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000036E4 File Offset: 0x000018E4
		private void SelectFish(ThingDef afishDef, int selindex)
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			string text = "Aquarium.DoNothing".Translate();
			list.Add(new FloatMenuOption(text, delegate()
			{
				this.FishSelection(false, null, 0, 0);
			}, MenuOptionPriority.Default, null, null, 29f, null, null));
			if (afishDef != null)
			{
				text = "Aquarium.RemoveFish".Translate();
				list.Add(new FloatMenuOption(text, delegate()
				{
					this.FishSelection(true, afishDef, selindex, 2);
				}, MenuOptionPriority.Default, null, null, 29f, null, null));
			}
			else
			{
				List<string> potentials = this.BagDefs();
				if (potentials.Count > 0)
				{
					foreach (string potential in potentials)
					{
						ThingDef potfishDef = ThingDef.Named(potential);
						text = "Aquarium.AddFish".Translate() + " " + potfishDef.LabelCap;
						list.Add(new FloatMenuOption(text, delegate()
						{
							this.FishSelection(true, potfishDef, selindex, 1);
						}, MenuOptionPriority.Default, null, null, 29f, null, null));
					}
				}
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003850 File Offset: 0x00001A50
		public void FishSelection(bool action, ThingDef selfishDef, int fishindex, int actionType)
		{
			if (action)
			{
				if (actionType == 1)
				{
					List<string> newFishData = new List<string>();
					int newindex = 0;
					if (this.fishData.Count > 0)
					{
						foreach (string value3 in this.fishData)
						{
							CompAquarium.numValuePart(value3, 0);
							string prevfishdefName = CompAquarium.stringValuePart(value3, 1);
							int prevHealth = CompAquarium.numValuePart(value3, 2);
							int prevAge = CompAquarium.numValuePart(value3, 3);
							int prevAction = CompAquarium.numValuePart(value3, 4);
							newindex++;
							string newValue = CompAquarium.CreateValuePart(newindex, prevfishdefName, prevHealth, prevAge, prevAction);
							newFishData.Add(newValue);
						}
					}
					newindex++;
					string addValue = CompAquarium.CreateValuePart(newindex, selfishDef.defName, 0, 0, 1);
					newFishData.Add(addValue);
					this.fishData = newFishData;
					return;
				}
				if (actionType == 2)
				{
					List<string> newFishData2 = new List<string>();
					bool cancel = false;
					if (this.fishData.Count > 0)
					{
						foreach (string value in this.fishData)
						{
							int row = CompAquarium.numValuePart(value, 0);
							string prevfishdefName2 = CompAquarium.stringValuePart(value, 1);
							int prevHealth2 = CompAquarium.numValuePart(value, 2);
							int prevAge2 = CompAquarium.numValuePart(value, 3);
							int prevAction2 = CompAquarium.numValuePart(value, 4);
							if (row == fishindex)
							{
								if (prevAction2 == 0)
								{
									string newValue2 = CompAquarium.CreateValuePart(row, prevfishdefName2, prevHealth2, prevAge2, 2);
									newFishData2.Add(newValue2);
								}
								else if (prevAction2 == 1)
								{
									string newValue3 = CompAquarium.CreateValuePart(row, prevfishdefName2, prevHealth2, prevAge2, 3);
									newFishData2.Add(newValue3);
									cancel = true;
								}
								else
								{
									newFishData2.Add(value);
								}
							}
							else
							{
								newFishData2.Add(value);
							}
						}
					}
					this.fishData = newFishData2;
					if (cancel)
					{
						List<string> cancelFishData = new List<string>();
						int newindex2 = 0;
						if (this.fishData.Count > 0)
						{
							foreach (string value2 in this.fishData)
							{
								newindex2++;
								int prevAction3 = CompAquarium.numValuePart(value2, 4);
								if (prevAction3 != 3)
								{
									string newValue4 = CompAquarium.CreateValuePart(newindex2, CompAquarium.stringValuePart(value2, 1), CompAquarium.numValuePart(value2, 2), CompAquarium.numValuePart(value2, 3), prevAction3);
									cancelFishData.Add(newValue4);
								}
								else
								{
									newindex2--;
								}
							}
						}
						this.fishData = cancelFishData;
					}
				}
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003AD0 File Offset: 0x00001CD0
		public override string CompInspectStringExtra()
		{
			return "Aquarium.TankInfo".Translate(this.cleanPct.ToStringPercent(), this.foodPct.ToStringPercent());
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003C24 File Offset: 0x00001E24
		private List<string> BagDefs()
		{
			var bagDefs = (from bagdef in DefDatabase<ThingDef>.AllDefsListForReading where bagdef.defName.StartsWith("AQFishInBag") select bagdef.defName).ToList();
			bagDefs.SortBy((string s) => ThingDef.Named(s).label);
			return bagDefs;
		}

		private static int WordsToNumbers(string word)
        {
            for (int i = 0; i < 500; i++)
            {
				if(NumberToWords(i) == word.ToLower())
                {
					return i;
                }
            }
			return 1;
        }

		private static string NumberToWords(int number)
		{
			if (number == 0)
				return "zero";

			if (number < 0)
				return "minus " + NumberToWords(Math.Abs(number));

			string words = "";

			if ((number / 1000000) > 0)
			{
				words += NumberToWords(number / 1000000) + " million ";
				number %= 1000000;
			}

			if ((number / 1000) > 0)
			{
				words += NumberToWords(number / 1000) + " thousand ";
				number %= 1000;
			}

			if ((number / 100) > 0)
			{
				words += NumberToWords(number / 100) + " hundred ";
				number %= 100;
			}

			if (number > 0)
			{
				if (words != "")
					words += "and ";

				var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
				var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

				if (number < 20)
					words += unitsMap[number];
				else
				{
					words += tensMap[number / 10];
					if ((number % 10) > 0)
						words += "-" + unitsMap[number % 10];
				}
			}

			return words;
		}


		// Token: 0x06000030 RID: 48 RVA: 0x00003CE8 File Offset: 0x00001EE8
		private bool IsPowered()
		{
			if (!this.parent.Spawned)
			{
				return false;
			}
			if (this.parent.IsBrokenDown())
			{
				return false;
			}
			CompPowerTrader CPT = this.parent.TryGetComp<CompPowerTrader>();
			return CPT != null && CPT.PowerOn;
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00003D2D File Offset: 0x00001F2D
		private bool ShouldPushHeatNow
		{
			get
			{
				return this.parent.SpawnedOrAnyParentSpawned && this.parent.AmbientTemperature < this.Props.targetTemp;
			}
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003D5C File Offset: 0x00001F5C
		internal void TankBreeding()
		{
			if (Controller.Settings.AllowBreed && this.numFish > 1 && this.numFish < this.Props.maxFish && this.fishData.Count > 0)
			{
				List<string> breeders = new List<string>();
				List<string> potentials = new List<string>();
				List<string> newFishList = new List<string>();
				int newindex = 0;
				bool birth = false;
				foreach (string fish in this.fishData)
				{
					newindex++;
					if (CompAquarium.numValuePart(fish, 3) >= CompAquarium.oldFishAge / 2 && CompAquarium.numValuePart(fish, 4) == 0)
					{
						string fishdef = CompAquarium.stringValuePart(fish, 1);
						if (breeders.Contains(fishdef))
						{
							potentials.AddDistinct(fishdef);
						}
						breeders.Add(fishdef);
					}
					newFishList.Add(fish);
				}
				if (potentials.Count > 0 && CompAquarium.RandomFloat(1f, 100f) <= Controller.Settings.BreedChance)
				{
					newindex++;
					string babyFishDef = potentials.RandomElement<string>();
					string newValue = CompAquarium.CreateValuePart(newindex, babyFishDef, 75, 0, 0);
					newFishList.Add(newValue);
					birth = true;
				}
				if (birth)
				{
					this.fishData = newFishList;
					this.numFish++;
				}
			}
		}

		// Token: 0x04000008 RID: 8
		private const int Ticks = 300;

		// Token: 0x04000009 RID: 9
		internal static int oldFishAge = 3600000 * (int)Controller.Settings.FishLife;

		// Token: 0x0400000A RID: 10
		internal const int FishAgeSpread = 450000;

		// Token: 0x0400000B RID: 11
		private const int BreedingTicks = 300000;

		// Token: 0x0400000C RID: 12
		internal int numFish;

		// Token: 0x0400000D RID: 13
		internal float cleanPct = 1f;

		// Token: 0x0400000E RID: 14
		internal float foodPct;

		// Token: 0x0400000F RID: 15
		internal List<string> fishData = new List<string>();

		// Token: 0x04000010 RID: 16
		internal bool fishydebug;

		// Token: 0x04000011 RID: 17
		private const float DegradeRateBalancer = 0.125f;

		// Token: 0x04000012 RID: 18
		public Sustainer mixSustainer;

		// Token: 0x04000013 RID: 19
		[NoTranslate]
		private string debugTexPath = "Things/Fish/UI/Debug_Icon";
	}
}
