using System.Collections.Generic;
using Verse;

namespace NomadsLand
{
    public abstract class WorldGenSteps_DefModExtension : DefModExtension
    {
        public List<WorldGenStepDef> extendedSteps;
    }

    public class WorldGenStepsAdd_DefModExtension : WorldGenSteps_DefModExtension { }

    public class WorldGenStepsReplace_DefModExtension : WorldGenSteps_DefModExtension { }
}
