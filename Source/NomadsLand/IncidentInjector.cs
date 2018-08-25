using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Harmony;
using RimWorld.Planet;

namespace NomadsLand
{
    public class IncidentInjector_GameComponent : GameComponent
    {
        public bool incidentInjector = false;

        public IncidentInjector_GameComponent() { }
        public IncidentInjector_GameComponent(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.incidentInjector, "incidentInjector", false);
        }
    }

    [StaticConstructorOnStartup]
    public class ScenPart_IncidentInjector : ScenPart
    {
        #region static -> harmony patching

        static ScenPart_IncidentInjector()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nomadsland.scenpartincidentinjector");
            harmony.Patch(AccessTools.Method(typeof(Game), "ExposeSmallComponents"), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(Init_ScenPart_IncidentInjector)));
        }

        public static void Init_ScenPart_IncidentInjector()
        {
            if (Current.Game.GetComponent<IncidentInjector_GameComponent>().incidentInjector)
                ScenPart_IncidentInjector.AddStorytellerComp();

        }

        #endregion

        public override void PreConfigure()
        {
            base.PreConfigure();
            Current.Game.GetComponent<IncidentInjector_GameComponent>().incidentInjector = true;
        }

        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            AddStorytellerComp();
        }

        public static void AddStorytellerComp()
        {
            Current.Game.storyteller.storytellerComps.Add(new StorytellerComp_IncidentInjector());
        }

    }

    public class StorytellerComp_IncidentInjector : StorytellerComp
    {
        public StorytellerComp_IncidentInjector()
        {
            this.props = new StorytellerCompProperties();
        }

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (target is World)
            {
                foreach(IncidentDef inc in DefDatabase<IncidentDef>.AllDefsListForReading.Where(inc => inc.targetTags.Contains(IncidentTargetTagDefOf.ScenPart)))
                {
                    Log.Message($"{inc}");
                    // TODO: these threat points will probably be off...
                    IncidentParms parms = this.GenerateParms(inc.category, target);
                    if (inc.Worker.CanFireNow(parms, false))
                    {
                        Log.Message($"Firing!");
                        yield return new FiringIncident(inc, this, parms);
                    }
                }

            }

        }
    }


}
