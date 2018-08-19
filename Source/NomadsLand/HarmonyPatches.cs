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
            harmony.Patch(AccessTools.Method(typeof(GameRules), nameof(GameRules.DesignatorAllowed)), new HarmonyMethod(typeof(HarmonyPatches), nameof(DesignatorAllowedPrefix)), null); //, new HarmonyMethod(typeof(DisallowAllBuildingScenario), nameof(DesignatorAllowedTranspiler)));
            harmony.Patch(AccessTools.Property(typeof(Faction), nameof(Faction.OfPlayer)).GetGetMethod(), new HarmonyMethod(typeof(HarmonyPatches), nameof(GetOfPlayerDetour)), null); 
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.SetForbidden)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SkipForbidding)), null);
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.SetForbiddenIfOutsideHomeArea)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SkipForbidding)), null);
            harmony.Patch(AccessTools.Method(typeof(ScenPart_PlayerFaction), nameof(ScenPart_PlayerFaction.PreMapGenerate)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SkipPlayerSettlementGeneration)), null);
            harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(CaravanStartTranspiler)));
#if DEBUG
            HarmonyInstance.DEBUG = false;
#endif
        }

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
                //curGame.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;
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

        public static bool CaravanStart { get => Current.Game.GetComponent<NomadsLand_RulesExt>().CaravanStart; }

        // Prefix Skipper => skips on false value
        public static bool SkipForbidding() => !Current.Game.GetComponent<NomadsLand_RulesExt>().NothingForbidden;
        public static bool SkipPlayerSettlementGeneration() => !CaravanStart;

        public static bool DesignatorAllowedPrefix(bool __result, Designator d)
        {
            if (d is Designator_Place)
            {
                // set result true, do not continue method execution
                if (Current.Game.GetComponent<NomadsLand_RulesExt>().DisallowBuildings)
                {
                    __result = false;
                    return false;
                }
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

    }

}
