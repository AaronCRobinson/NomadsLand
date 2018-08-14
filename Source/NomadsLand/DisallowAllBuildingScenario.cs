using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace NomadsLand
{
    [StaticConstructorOnStartup]
    public static class DisallowAllBuildingScenario
    {
        static DisallowAllBuildingScenario()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nomadsland.disallowallbuildingscenario");
            harmony.Patch(AccessTools.Method(typeof(RimWorld.GameRules), nameof(RimWorld.GameRules.DesignatorAllowed)), new HarmonyMethod(typeof(DisallowAllBuildingScenario), nameof(DesignatorAllowedPrefix)), null); //, new HarmonyMethod(typeof(DisallowAllBuildingScenario), nameof(DesignatorAllowedTranspiler)));
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

    }

}
