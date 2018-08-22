using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace NomadsLand
{
    public class Rule_DisallowAllBuilding : ScenPart_Rule
    {
        protected override void ApplyRule() => Current.Game.GetComponent<NomadsLand_RulesExt>().DisallowBuildings = true;
    }

    public class Rule_MapsGenerateIncidents : ScenPart_Rule
    {
        protected override void ApplyRule() => Current.Game.GetComponent<NomadsLand_RulesExt>().MapsGenerateIncidents = true;
    }

    public class Rule_NothingForbidden : ScenPart_Rule
    {
        protected override void ApplyRule() => Current.Game.GetComponent<NomadsLand_RulesExt>().NothingForbidden = true;
    }

    // TODO: consider adding a variable here for the multiplyer used in extending the timer.
    public class Rule_ExtendForceExitTimer : ScenPart_Rule
    {
        protected override void ApplyRule() => Current.Game.GetComponent<NomadsLand_RulesExt>().ExtendForceExitTimer = true;
    }

    // It would be nice to consolidate this with ScenPart_PlayerPawnsArriveMethod
    public class ScenPart_CaravanStart : ScenPart
    {
        public override void PreConfigure()
        {
            base.PreConfigure();
            Current.Game.GetComponent<NomadsLand_RulesExt>().CaravanStart = true;
        }
    }

    public class ScenPart_GenStepPrisonerRescues : ScenPart
    {
        private FloatRange prisonerRescuePer100kTiles = new FloatRange(65f, 75f);

        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            ScenPart_Helper.GeneratePrisonerRecuesIntoWorld(prisonerRescuePer100kTiles);
        }

        public override void PostGameStart()
        {
            base.PostGameStart();
            foreach (Site site in Find.WorldObjects.Sites.Where(s => s.core.def == SiteCoreDefOf.PrisonerWillingToJoin))
            {
                Pawn pawn = PrisonerWillingToJoinQuestUtility.GeneratePrisoner(site.Tile, site.Faction);
                site.GetComponent<PrisonerWillingToJoinComp>().pawn.TryAdd(pawn, true);
            }
        }
    }


    public abstract class GuasianAsymmThreatScenPart : ScenPart
    {
        protected float meanThreatValue = 500;
        protected float threatVariance = 1500;
        protected SimpleCurve threatDensity = new SimpleCurve
        {
            { new CurvePoint(0f, 0f), true },
            { new CurvePoint(100f, 500f), true },
            { new CurvePoint(500f, 50f), true }
        };
    }

    public class ScenPart_GenStepOutposts : GuasianAsymmThreatScenPart
    {
        private FloatRange outpostsPer100kTiles = new FloatRange(135f, 165f);

        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            GenerateOutpostsIntoWorld();
        }

        public void GenerateOutpostsIntoWorld()
        {
            int total = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * outpostsPer100kTiles.RandomInRange);
            int cnt = 0;
            Log.Message($"Total: {total}");
            while (cnt < total)
            {
                float threatLevel = Rand.Gaussian(meanThreatValue, threatVariance); 
                int num = (int)threatDensity.Evaluate(threatLevel) / 20 + 1; // helps get greater spread
                Log.Message($"Outposts - Threat {threatLevel}, number {num}");
                for (int k = 0; k < num && cnt < total; k++, cnt++)
                {
                    Faction faction = (from x in Find.World.factionManager.AllFactionsListForReading
                                       where !x.def.isPlayer && !x.def.hidden
                                       select x).RandomElementByWeight((Faction x) => x.def.settlementGenerationWeight);
                    int tile = TileFinder.RandomSettlementTileFor(faction, false, null);
                    Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.Outpost, tile, faction, true, threatLevel);
                    site.sitePartsKnown = true;
                    Find.WorldObjects.Add(site);
                }
            }
        }
    }

    public class ScenPart_GenStepItemStashes : ScenPart
    {
        private FloatRange itemStashesPer100kTiles = new FloatRange(60f, 80f);

        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            ScenPart_Helper.GenerateItemStashesIntoWorld(itemStashesPer100kTiles);
        }
    }

    public static class ScenPart_Helper
    {
        public static void GeneratePrisonerRecuesIntoWorld(FloatRange densityRange)
        {
            int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * densityRange.RandomInRange);
            for (int k = 0; k < num; k++)
            {
                Faction faction = (from x in Find.World.factionManager.AllFactionsListForReading
                                    where !x.def.isPlayer && !x.def.hidden
                                    select x).RandomElementByWeight((Faction x) => x.def.settlementGenerationWeight);
                int tile = TileFinder.RandomSettlementTileFor(faction, false, null);
                Site site = SiteMaker.TryMakeSite_SingleSitePart(SiteCoreDefOf.PrisonerWillingToJoin, "PrisonerRescueQuestThreat", tile, faction, true, null, true, null);
                if (site == null) continue;
                site.sitePartsKnown = true;
                Find.WorldObjects.Add(site);
            }
        }

        public static void GenerateItemStashesIntoWorld(FloatRange densityRange)
{
            int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * densityRange.RandomInRange);
            for (int k = 0; k < num; k++)
            {
                if (!SiteMakerHelper.TryFindSiteParams_SingleSitePart(SiteCoreDefOf.ItemStash, (!Rand.Chance(0.18f)) ? "ItemStashQuestThreat" : null, out SitePartDef sitePart, out Faction siteFaction, null, true, null))
                    continue;

                int tile = TileFinder.RandomSettlementTileFor(siteFaction, false, null);
                Site site = SiteMaker.TryMakeSite_SingleSitePart(SiteCoreDefOf.ItemStash, new List<SitePartDef>() { sitePart }, tile, siteFaction, true, null, true, null);

                if (site == null) continue;
                site.sitePartsKnown = true;
                Find.WorldObjects.Add(site);
            }
        }
    }
}
