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
        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            ScenPart_Helper.GeneratePrisonerRecuesIntoWorld();
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

    public class ScenPart_GenStepOutposts : ScenPart
    {
        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            ScenPart_Helper.GenerateOutpostsIntoWorld();
        }
    }

    public class ScenPart_GenStepItemStashes : ScenPart
    {
        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            ScenPart_Helper.GenerateItemStashesIntoWorld();
        }
    }


    public static class ScenPart_Helper
    {
        // TODO: include these in the defs as variable
        private static readonly FloatRange OutpostsPer100kTiles = new FloatRange(135f, 165f);
        private static readonly FloatRange PrisonerRescuePer100kTiles = new FloatRange(65f, 75f);
        private static readonly FloatRange ItemStashesPer100kTiles = new FloatRange(60f, 80f);

        public static void GenerateOutpostsIntoWorld()
        {
            int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * OutpostsPer100kTiles.RandomInRange);
            for (int k = 0; k < num; k++)
            {
                Faction faction = (from x in Find.World.factionManager.AllFactionsListForReading
                                    where !x.def.isPlayer && !x.def.hidden
                                    select x).RandomElementByWeight((Faction x) => x.def.settlementGenerationWeight);
                int tile = TileFinder.RandomSettlementTileFor(faction, false, null);
                Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.Outpost, tile, faction, true, null);
                site.sitePartsKnown = true;
                Find.WorldObjects.Add(site);
            }
        }

        public static void GeneratePrisonerRecuesIntoWorld()
        {
            int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * PrisonerRescuePer100kTiles.RandomInRange);
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

        public static void GenerateItemStashesIntoWorld()
        {
            int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * ItemStashesPer100kTiles.RandomInRange);
            for (int k = 0; k < num; k++)
            {
                if (!SiteMakerHelper.TryFindSiteParams_SingleSitePart(SiteCoreDefOf.ItemStash, (!Rand.Chance(0.18f)) ? "ItemStashQuestThreat" : null, out SitePartDef sitePart, out Faction siteFaction, null, true, null))
                    continue;

                int tile = TileFinder.RandomSettlementTileFor(siteFaction, false, null);
                Site site = SiteMaker.TryMakeSite_SingleSitePart(SiteCoreDefOf.ItemStash, new List<SitePartDef>() { sitePart }, tile, siteFaction, true, null, true, null);
                //Site site = SiteMaker.MakeSite(SiteCoreDefOf.ItemStash, sitePart, tile, siteFaction, true, null);

                if (site == null) continue;
                site.sitePartsKnown = true;
                Find.WorldObjects.Add(site);
            }
        }
    }
}
