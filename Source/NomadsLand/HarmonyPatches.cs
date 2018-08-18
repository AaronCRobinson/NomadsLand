using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using RimWorld;
using Harmony;

// TODO: look for transpiler solutions and stop being dirty and lazy.
namespace NomadsLand
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nomadsland.disallowallbuildingscenario");
            harmony.Patch(AccessTools.Method(typeof(GameRules), nameof(GameRules.DesignatorAllowed)), new HarmonyMethod(typeof(HarmonyPatches), nameof(DesignatorAllowedPrefix)), null); //, new HarmonyMethod(typeof(DisallowAllBuildingScenario), nameof(DesignatorAllowedTranspiler)));
            harmony.Patch(AccessTools.Property(typeof(Faction), nameof(Faction.OfPlayer)).GetGetMethod(), new HarmonyMethod(typeof(HarmonyPatches), nameof(GetOfPlayerDetour)), null); 
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.SetForbiddenIfOutsideHomeArea)), new HarmonyMethod(typeof(HarmonyPatches), nameof(ForbiddenPrefix)), null); 
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.SetForbidden)), new HarmonyMethod(typeof(HarmonyPatches), nameof(ForbiddenPrefix)), null);
        }

        public static bool ForbiddenPrefix()
        {
            if (Current.Game.GetComponent<NomadsLand_RulesExt>().NothingForbidden)
                return false;
            return true;
        }

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
