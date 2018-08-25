using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Harmony;

// TODO: look for transpiler solutions and stop being dirty and lazy.
namespace NomadsLand
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nomadsland.disallowallbuildingscenario");
            harmony.Patch(AccessTools.Method(typeof(GameRules), nameof(GameRules.DesignatorAllowed)), new HarmonyMethod(typeof(HarmonyPatches), nameof(DesignatorAllowedPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.SetForbidden)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SkipForbidding)), null);
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.SetForbiddenIfOutsideHomeArea)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SkipForbidding)), null);
            harmony.Patch(AccessTools.Method(typeof(ScenPart_PlayerFaction), nameof(ScenPart_PlayerFaction.PreMapGenerate)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SkipPlayerSettlementGeneration)), null);
            harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(CaravanStartTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(WorldObject), nameof(WorldObject.GetInspectString)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(FactionNameSwitchRoo)));

            //harmony.Patch(AccessTools.Method(typeof(HistoryAutoRecorderWorker_WealthTotal), nameof(HistoryAutoRecorderWorker_WealthTotal.PullRecord)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(ReplaceWealthTotalTranspiler)));

            //harmony.Patch(AccessTools.Method(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount)), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(WealthWatcher_ForceRecount_Postfix)));
            // TODO: breakdown into pawns and items
            //harmony.Patch(AccessTools.Property(typeof(WealthWatcher), nameof(WealthWatcher.WealthTotal)).GetGetMethod(), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(AdjustWealthTotal)));
            //harmony.Patch(AccessTools.Property(typeof(WealthWatcher), nameof(WealthWatcher.WealthTotal)).GetGetMethod(), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(AdjustWealthTotal)));
            //harmony.Patch(AccessTools.Property(typeof(WealthWatcher), nameof(WealthWatcher.WealthTotal)).GetGetMethod(), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TranspilerTest)));

            // NOTE: this is eating error messages (they don't appear to be significant)
            harmony.Patch(AccessTools.Property(typeof(Faction), nameof(Faction.OfPlayer)).GetGetMethod(), new HarmonyMethod(typeof(HarmonyPatches), nameof(GetOfPlayerDetour)), null);
#if DEBUG
            HarmonyInstance.DEBUG = false;
#endif
        }

        /*private static MethodInfo MI_WealthTotal = AccessTools.Property(typeof(WealthWatcher), nameof(WealthWatcher.WealthTotal)).GetGetMethod();
        private static MethodInfo MI_ReplacementWealthTotal = AccessTools.Method(typeof(HarmonyPatches), nameof(get_WealthTotal));
        public static IEnumerable<CodeInstruction> ReplaceWealthTotalTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(CodeInstruction instruction in instructions)
            {
                if (instruction.operand == MI_WealthTotal)
                    yield return new CodeInstruction(OpCodes.Call, MI_ReplacementWealthTotal);
                else
                    yield return instruction;
            }
        }

        // NOTE: beware of places this will lead to multiple counts of caravan wealth...
        public static float get_WealthTotal(WealthWatcher wealthWatcher)
        {
            Log.Message("get_WealthTotal");
            float total = wealthWatcher.WealthTotal;
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
                total += caravan.PlayerWealthForStoryteller;
            Log.Message($"{total}");
            return total;
        }*/

        private static readonly MethodInfo MI_RecountIfNeeded = AccessTools.Method(typeof(WealthWatcher), "RecountIfNeeded");
        private static readonly FieldInfo FI_wealthPawns = AccessTools.Field(typeof(WealthWatcher), "wealthPawns");
        private static readonly FieldInfo FI_wealthItems = AccessTools.Field(typeof(WealthWatcher), "wealthItems");
        private static readonly FieldInfo FI_wealthBuildings = AccessTools.Field(typeof(WealthWatcher), "wealthBuildings");
        public static bool AdjustWealthTotal(WealthWatcher __instance, ref float __result)
        {
            Log.Message("AdjustWealthTotal");
            MI_RecountIfNeeded.Invoke(__instance, new object[] { });
            __result = (float)FI_wealthPawns.GetValue(__instance) + (float)FI_wealthBuildings.GetValue(__instance) + (float)FI_wealthPawns.GetValue(__instance);
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
                __result += caravan.PlayerWealthForStoryteller;
            return false;
        }
        //private static readonly FieldInfo FI_wealthPawns = AccessTools.Field(typeof(WealthWatcher), "wealthPawns");
        //private static readonly FieldInfo FI_wealthItems = AccessTools.Field(typeof(WealthWatcher), "wealthItems");
        // This adds tracking of  caravan pawns and items in WealthWatcher
        /*public static void WealthWatcher_ForceRecount_Postfix(WealthWatcher __instance)
        {
            float wealthPawns = (float)FI_wealthPawns.GetValue(__instance);
            float wealthItems = (float)FI_wealthItems.GetValue(__instance);
            Log.Message($"Starting - {wealthPawns} {wealthItems}");
            foreach(Caravan caravan in Find.WorldObjects.Caravans)
            {
                foreach(Thing thing in caravan.AllThings)
                {
                    if (thing is Pawn pawn)
                        wealthPawns += pawn.MarketValue;
                    else
                        wealthItems += thing.MarketValue;
                }
            }
            Log.Message($"Ending - {wealthPawns} {wealthItems}");
            FI_wealthPawns.SetValue(__instance, wealthPawns);
            FI_wealthItems.SetValue(__instance, wealthItems);
        }*/

        public static IEnumerable<CodeInstruction> CaravanStartTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            FieldInfo FI_initialMapSize = AccessTools.Field(typeof(WorldInfo), nameof(WorldInfo.initialMapSize));
            MethodInfo MI_CameraDriver_getter = AccessTools.Property(typeof(Find), nameof(Find.CameraDriver)).GetGetMethod();
            List<CodeInstruction> codeInstructions = instructions.ToList();

            Label endOfIf = il.DefineLabel();

            for (int i=0; i<codeInstructions.Count; i++)
            {
                CodeInstruction instruction = codeInstructions[i];
                if (instruction.opcode == OpCodes.Ldnull && codeInstructions[i+1].opcode == OpCodes.Stloc_2) // start if block
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(HarmonyPatches), nameof(CaravanStart)).GetGetMethod());
                    Label settlementBlock = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse, settlementBlock);

                    // TODO: do caravan start
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CaravanStartTranspiler_Helper), nameof(CaravanStartTranspiler_Helper.HandleCarvanStart)));

                    // break to end of if
                    yield return new CodeInstruction(OpCodes.Br, endOfIf);
                    
                    instruction.labels.Add(settlementBlock);

                } else if (instruction.opcode == OpCodes.Stfld && instruction.operand == FI_initialMapSize) {
                    yield return instruction;
                    // ASSUMING: not out of bounds
                    codeInstructions[i+1].labels.Add(endOfIf);
                    continue;
                } else if (instruction.opcode == OpCodes.Call && instruction.operand == MI_CameraDriver_getter) // rip out CameraDriver
                {
                    while (codeInstructions[++i].opcode != OpCodes.Callvirt) { }
                    continue; // skips the current Callvirt call (i++ in the loop)
                }
                yield return instruction;
            }
        }

        public static class CaravanStartTranspiler_Helper
        {
            public static void HandleCarvanStart(Game curGame)
            {
                // NOTE: Second time called. Helps align sun for correct time.
                curGame.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;
                Caravan caravan = CaravanMaker.MakeCaravan(Find.GameInitData.startingAndOptionalPawns, Faction.OfPlayer, Find.GameInitData.startingTile, true);
                // add items to caravans (animals still need to be added to world)
                foreach (Thing thing in Find.Scenario.AllParts.SelectMany(scen => scen.PlayerStartingThings()))
                {
                    if (thing is Pawn pawn && !pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                        pawn.SetFactionDirect(Faction.OfPlayer);
                    }
                    caravan.AddPawnOrItem(thing, false);
                }
                Find.WorldCameraDriver.JumpTo(Find.GameInitData.startingTile);
            }
        }

        public static bool CaravanStart { get => Current.Game.GetComponent<NomadsLand_RulesExt>().caravanStart; }

        // Prefix Skipper => skips on false value
        public static bool SkipForbidding() => !Current.Game.GetComponent<NomadsLand_RulesExt>().nothingForbidden;
        public static bool SkipPlayerSettlementGeneration() => !CaravanStart;

        // NOTE: need to consider mod compatibilitity (maybe easier to white list allowed decorators - lol)
        private static readonly MethodInfo MI_DesignationGetter = AccessTools.Property(typeof(Designator), "Designation").GetGetMethod();
        private static readonly HashSet<BuildableDef> WhiteListedBuildableDefs = new HashSet<BuildableDef>() { ThingDefOf.Campfire, ThingDefOf.TorchLamp, ThingDefOf.Sandbags, ThingDefOf.Turret_Mortar };
        public static bool DesignatorAllowedPrefix(bool __result, Designator d)
        {
            bool setFalseResult() { __result = false; return false; }

            if (Current.Game.GetComponent<NomadsLand_RulesExt>().disallowBuildings)
            {
                if (d is Designator_Place dp)
                {
                    // set result true, do not continue method execution
                    if (!dp.PlacingDef.defName.Contains("Spot") && !dp.PlacingDef.defName.Contains("Trap") && !WhiteListedBuildableDefs.Contains(dp.PlacingDef))
                        return setFalseResult();
                    //return true; // whitelisted items
                }
                else if (d is Designator_Dropdown dd)
                {
                    if (dd.Elements.First() is Designator_Place dpp  && dpp.PlacingDef.designationCategory == DesignationCategoryDefOf.Floors)
                        return setFalseResult();
                }
                else if (d is Designator_SmoothSurface || d is Designator_RemoveFloor || d is Designator_AreaBuildRoof || d is Designator_AreaNoRoof || d is Designator_AreaIgnoreRoof)
                    return setFalseResult();
            }
            return true;
        }

        // Eating error messages (they don't appear to be significant)
        public static bool GetOfPlayerDetour(ref Faction __result)
        {
            __result = Faction.OfPlayerSilentFail;
            if (__result == null)
                Log.Message("Warning: Could not find player faction.");
            return false;
        }

        public static IEnumerable<CodeInstruction> FactionNameSwitchRoo(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo MI_NameGetter = AccessTools.Property(typeof(Faction), nameof(Faction.Name)).GetGetMethod();
            MethodInfo MI_NameDetour = AccessTools.Method(typeof(HarmonyPatches), nameof(ExtendingName));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.operand == MI_NameGetter)
                    yield return new CodeInstruction(instruction.opcode, MI_NameDetour);
                else
                    yield return instruction;
            }
        }

        public static string ExtendingName(Faction faction) => $"{faction.Name} ({faction.RelationWith(Faction.OfPlayer).kind})";

    }

}
