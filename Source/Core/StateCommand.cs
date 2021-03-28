using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Puppeteer
{
	public static class StateCommand
	{
		public static void Set(Connection connection, IncomingState state)
		{
			if (connection == null) return;
			var vID = state.user;
			if (vID == null) return;
			var puppeteer = State.Instance.PuppeteerForViewer(vID);
			var pawn = puppeteer?.puppet?.pawn;
			if (pawn == null || pawn.Spawned == false) return;

			if (puppeteer != null)
			{
				puppeteer.lastCommandIssued = DateTime.Now;
				puppeteer.lastCommand = $"set-{state.key}";
			}

			var settings = PawnSettings.SettingsFor(pawn);
			try
			{
				switch (state.key)
				{
					case "hostile-response":
						if (settings.enabled == false) return;
						var responseMode = (HostilityResponseMode)Enum.Parse(typeof(HostilityResponseMode), state.val.ToString());
						pawn.playerSettings.hostilityResponse = responseMode;
						pawn.RemoteLog($"Response Mode to {responseMode}");
						break;
					case "drafted":
						if (settings.enabled == false) return;
						var drafted = Convert.ToBoolean(state.val);
						if (Tools.CannotMoveOrDo(pawn) == false)
							pawn.FakeDraft(drafted);
						pawn.RemoteLog(drafted ? "Drafted" : "Undrafted");
						break;
					case "zone":
						if (settings.enabled == false) return;
						var area = pawn.Map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()).FirstOrDefault(a => a.Label == state.val.ToString());
						pawn.playerSettings.AreaRestriction = area;
						pawn.RemoteLog(area == null ? "Area unrestricted" : $"Area restricted to {area.Label}");
						break;
					case "priority":
						{
							if (settings.enabled == false) return;
							var val = Convert.ToInt32(state.val);
							var idx = val / 100;
							var prio = val % 100;
							var defs = Integrations.GetPawnWorkerDefs().ToArray();
							if (idx >= 0 && idx < defs.Length)
							{
								pawn.workSettings.SetPriority(defs[idx], prio);
								pawn.RemoteLog($"Changed {defs[idx].label} priority to {prio}");
							}
							break;
						}
					case "schedule":
						{
							if (settings.enabled == false) return;
							var pair = Convert.ToString(state.val).Split(':');
							if (pair.Length == 2)
							{
								var idx = Tools.SafeParse(pair[0]);
								if (idx.HasValue)
								{
									var type = Defs.Assignments.FirstOrDefault(ass => ass.Value == pair[1]).Key;
									if (type != null)
									{
										pawn.timetable.SetAssignment(idx.Value, type);
										pawn.RemoteLog($"Defined {idx.Value}h to {type.label}");
									}
									else
										GeneralCommands.SendSchedules(connection);
								}
							}
							break;
						}
					case "grid":
						{
							var grid = Tools.SafeParse(state.val, 4);
							Renderer.RenderMap(puppeteer, grid);
							break;
						}
					case "goto":
						{
							if (settings.enabled == false) return;
							var val = Convert.ToString(state.val);
							var coordinates = val.Split(',').Select(v => { if (int.TryParse(v, out var n)) return n; else return -1000; }).ToArray();
							if (coordinates.Length == 2)
							{
								if (Tools.CannotMoveOrDo(pawn) == false)
								{
									var cell = new IntVec3(coordinates[0], 0, coordinates[1]);
									if (cell.InBounds(pawn.Map) && cell.Standable(pawn.Map))
									{
										var job = JobMaker.MakeJob(JobDefOf.Goto, cell);
										pawn.FakeDraft(true);
										pawn.jobs.StartJob(job, JobCondition.InterruptForced);
										pawn.RemoteLog($"Drafted to {cell.x}x{cell.z}");
									}
								}
							}
							break;
						}
					case "menu":
						{
							var val = Convert.ToString(state.val);
							var coordinates = val.Split(',').Select(v => { if (int.TryParse(v, out var n)) return n; else return -1000; }).ToArray();
							if (coordinates.Length == 2)
							{
								Actions.RemoveActions(pawn);
								if (Tools.CannotMoveOrDo(pawn) == false)
								{
									var cell = new IntVec3(coordinates[0], 0, coordinates[1]);
									if (cell.InBounds(pawn.Map))
									{
										var choices = Array.Empty<ContextMenu.Choice>();
										using (new MapFaker(pawn))
										{
											choices = FloatMenuMakerMap
												.ChoicesAtFor(cell.ToVector3(), pawn)
												.Select(choice =>
												{
													var id = Guid.NewGuid().ToString();
													Actions.AddAction(pawn, id, choice);
													var restricted = Tools.Restricted(pawn, cell, choice.Label);
													return new ContextMenu.Choice()
													{
														id = id,
														label = choice.Label + (restricted != null ? $" [{restricted}]" : ""),
														disabled = choice.Disabled || restricted != null
													};
												}).ToArray();
										}
										connection.Send(new ContextMenu()
										{
											controller = vID,
											choices = choices
										});
									}
								}
							}
							break;
						}
					case "action":
						{
							if (settings.enabled == false) return;
							var id = Convert.ToString(state.val);
							_ = Actions.RunAction(pawn, id);
							break;
						}
					case "select":
						{
							var val = Convert.ToString(state.val);
							var coordinates = val.Split(',').Select(v => { if (int.TryParse(v, out var n)) return n; else return -1000; }).ToArray();
							if (coordinates.Length == 2)
							{
								GizmosHandler.RemoveActions(pawn);

								var map = pawn.Map;
								var cell = new IntVec3(coordinates[0], 0, coordinates[1]);
								if (cell.InBounds(map))
								{
									var things = Selector.SelectableObjectsAt(cell, map);
									var obj = things.FirstOrDefault();
									var commands = GizmosHandler.GetCommands(pawn, cell, obj);
									if (obj == null || commands == null || commands.Count == 0)
									{
										connection.Send(new Selection()
										{
											controller = vID,
											frame = Array.Empty<Selection.Corner>(),
											gizmos = Array.Empty<Selection.Gizmo>(),
											atlas = Array.Empty<byte>(),
										});
										return;
									}

									void renderOp()
									{
										var atlas = Renderer.GetCommandsMatrix(commands);
										if (atlas == null)
										{
											Log.Warning("Retrying (should never happen)...");
											OperationQueue.Add(OperationType.Select, renderOp);
											return;
										}

										var bracketLocs = new Vector3[4];
										if (obj is Zone zone)
										{
											var x1 = zone.Cells.Min(c => c.x);
											var z1 = zone.Cells.Min(c => c.z);
											var x2 = zone.Cells.Max(c => c.x);
											var z2 = zone.Cells.Max(c => c.z);
											bracketLocs[0] = new Vector3(x1, 0, z1);
											bracketLocs[1] = new Vector3(x2, 0, z1);
											bracketLocs[2] = new Vector3(x2, 0, z2);
											bracketLocs[3] = new Vector3(x1, 0, z2);
										}
										else if (obj is Thing thing)
										{
											var customRectForSelector = thing.CustomRectForSelector;
											var selectTimes = new Dictionary<object, float>(); // cannot use SelectionDrawer.selectTimes because it would interfer with real selection
											if (customRectForSelector != null)
											{
												SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld(bracketLocs, thing, customRectForSelector.Value.CenterVector3, new Vector2((float)customRectForSelector.Value.Width, (float)customRectForSelector.Value.Height), selectTimes, Vector2.one, 1f);
											}
											else
												SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld<object>(bracketLocs, thing, thing.DrawPos, thing.RotatedSize.ToVector2(), selectTimes, Vector2.one, 1f);
										}

										var gizmos = Array.Empty<Selection.Gizmo>();
										var actions = new List<GizmosHandler.Item>();
										using (new MapFaker(pawn))
										{
											actions = GizmosHandler.GetActions(obj, commands);
											gizmos = actions
												.Select(gizmo =>
												{
													var id = Guid.NewGuid().ToString();
													GizmosHandler.AddAction(pawn, id, gizmo, obj as Thing);
													var restricted = Tools.Restricted(pawn, cell, gizmo.label);
													return new Selection.Gizmo()
													{
														id = id,
														label = gizmo.label,
														disabled = restricted ?? gizmo.disabled,
														allowed = gizmo.allowed && restricted == null,
													};
												}).ToArray();
										}
										connection.Send(new Selection()
										{
											controller = vID,
											frame = bracketLocs.Select(loc => new Selection.Corner(loc)).ToArray(),
											gizmos = gizmos,
											atlas = atlas
										});
									}

									OperationQueue.Add(OperationType.Select, renderOp);
								}
							}
							break;
						}
					case "gizmo":
						{
							if (settings.enabled == false) return;
							var id = Convert.ToString(state.val);
							_ = GizmosHandler.RunAction(pawn, id);
							break;
						}
					case "consume":
						{
							if (settings.enabled == false) return;
							var id = Convert.ToString(state.val);
							var thing = pawn.inventory.GetDirectlyHeldThings().FirstOrDefault(t => t.ThingID == id);
							if (thing != null)
							{
								var count = Mathf.Min(thing.stackCount, thing.def.ingestible.maxNumToIngestAtOnce);
								var job = new Job(JobDefOf.Ingest, thing) { count = count };
								job.count = Mathf.Min(job.count, FoodUtility.WillIngestStackCountOf(pawn, thing.def, thing.GetStatValue(StatDefOf.Nutrition, true)));
								_ = pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
							}
							break;
						}
					case "drop":
						{
							if (settings.enabled == false) return;
							var id = Convert.ToString(state.val);
							var thing = pawn.inventory.GetDirectlyHeldThings().FirstOrDefault(t => t.ThingID == id);
							if (thing != null)
							{
								_ = pawn.inventory.innerContainer.TryDrop(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out var _, null, null);
								break;
							}
							thing = pawn.equipment.GetDirectlyHeldThings().FirstOrDefault(t => t.ThingID == id);
							if (thing != null)
							{
								_ = pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thing), JobTag.Misc);
								break;
							}
							thing = pawn.apparel.GetDirectlyHeldThings().FirstOrDefault(t => t.ThingID == id);
							if (thing != null)
							{
								_ = pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, thing), JobTag.Misc);
								break;
							}
							break;
						}
					default:
						Tools.LogWarning($"Unknown set value operation with key {state.key}");
						break;
				}

			}
			catch (Exception ex)
			{
				Tools.LogError($"Exception while processing state command {state.key} for {pawn.LabelCap}: {ex}");
			}
		}
	}
}
