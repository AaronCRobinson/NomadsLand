using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using RimWorld;
using Harmony;
using RimWorld.Planet;

// TODO: look for transpiler solutions and stop being dirty and lazy.
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

    [StaticConstructorOnStartup]
    public static class WorldGenerator_Patches
    {
        static WorldGenerator_Patches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nomadsland.worldgeneratorpatches");
            //harmony.Patch(AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GenerateWorld)), new HarmonyMethod(typeof(WorldGenerator_Patches), nameof(GenerateWorldPrefix)), null); 
        }

        // NOTE: only works with predefined defs
        public static bool GenerateWorldPrefix(ref World __result, float planetCoverage, string seedString, OverallRainfall overallRainfall, OverallTemperature overallTemperature)
        {
            ScenarioDef def = DefDatabase<ScenarioDef>.AllDefs.First(d => d.scenario == Current.Game.Scenario);
            if (def.modExtensions.Any(ext => ext is WorldGenSteps_DefModExtension))
            {
                GenerateWorld_Helper.currentScenarioDef = def;
                __result = GenerateWorld_Helper.GenerateWorld(planetCoverage, seedString, overallRainfall, overallTemperature);
                return false;
            }
            return true;
        }
    }

    [StaticConstructorOnStartup]
    public static class GenerateWorld_Helper
    {
        public static ScenarioDef currentScenarioDef;
        private static readonly FieldInfo FI_tmpGenSteps = AccessTools.Field(typeof(WorldGenerator), "tmpGenSteps");
        private static List<WorldGenStepDef> tmpGenSteps;
        
        static GenerateWorld_Helper() => tmpGenSteps = FI_tmpGenSteps.GetValue(null) as List<WorldGenStepDef>;

        // we no profile
        public static World GenerateWorld(float planetCoverage, string seedString, OverallRainfall overallRainfall, OverallTemperature overallTemperature)
        {
            Rand.PushState();
            int seedFromSeedString = GenText.StableStringHash(seedString); //WorldGenerator.GetSeedFromSeedString(seedString);
            Rand.Seed = seedFromSeedString;
            World creatingWorld;
            try
            {
                // TODO: reimplement GenStepsInOrder here
                List<WorldGenStepDef> extendSteps = currentScenarioDef.GetModExtension<WorldGenSteps_DefModExtension>().extendedSteps;
                Current.CreatingWorld = new World();
                Current.CreatingWorld.info.seedString = seedString;
                Current.CreatingWorld.info.planetCoverage = planetCoverage;
                Current.CreatingWorld.info.overallRainfall = overallRainfall;
                Current.CreatingWorld.info.overallTemperature = overallTemperature;
                Current.CreatingWorld.info.name = NameGenerator.GenerateName(RulePackDefOf.NamerWorld, null, false, null, null);
                tmpGenSteps.Clear();
                if (currentScenarioDef.HasModExtension<WorldGenStepsAdd_DefModExtension>())
                    tmpGenSteps.AddRange(WorldGenerator.GenStepsInOrder);
                tmpGenSteps.AddRange(extendSteps);

                for (int i = 0; i < tmpGenSteps.Count; i++)
                {
                    try
                    {
                        Rand.Seed = Gen.HashCombineInt(seedFromSeedString, GenerateWorld_Helper.GetSeedPart(tmpGenSteps, i));
                        tmpGenSteps[i].worldGenStep.GenerateFresh(seedString);
                    }
                    catch (Exception arg)
                    {
                        Log.Error("Error in WorldGenStep: " + arg, false);
                    }
                }
                Rand.Seed = seedFromSeedString;
                Current.CreatingWorld.grid.StandardizeTileData();
                Current.CreatingWorld.FinalizeInit();
                Find.Scenario.PostWorldGenerate();
                creatingWorld = Current.CreatingWorld;
            }
            finally
            {
                Rand.PopState();
                Current.CreatingWorld = null;
            }
            return creatingWorld;
        }

        private static int GetSeedPart(List<WorldGenStepDef> genSteps, int index)
        {
            int seedPart = genSteps[index].worldGenStep.SeedPart;
            int num = 0;
            for (int i = 0; i < index; i++)
            {
                if (tmpGenSteps[i].worldGenStep.SeedPart == seedPart)
                {
                    num++;
                }
            }
            return seedPart + num;
        }

    }


}
