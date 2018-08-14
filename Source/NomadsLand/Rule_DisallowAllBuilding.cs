using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace NomadsLand
{
    public class Rule_DisallowAllBuilding : ScenPart_Rule
    {
        protected override void ApplyRule()
        {
            Current.Game.GetComponent<NomadsLand_RulesExt>().DisallowBuildings = true;
        }
    }
}
