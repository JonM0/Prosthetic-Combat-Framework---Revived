using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace OrenoPCF
{
    public class Command_HediffVerbRanged : Command
    {
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach (FloatMenuOption o in base.RightClickFloatMenuOptions)
                {
                    yield return o;
                }
                if (this.rangedComp.AllVerbs != null)
                {
                    foreach (Verb verb in this.rangedComp.AllVerbs.Where(verbs => !verbs.IsMeleeAttack))
                    {
                        string verbLabel = verb.verbProps.label.CapitalizeFirst();
                        void selectVerb()
                        {
                            this.rangedComp.rangedVerb = verb;
                            List<PCF_VerbProperties> verbProperties = this.rangedComp.Props.verbsProperties;
                            for (int i = 0; i < verbProperties.Count; i++)
                            {
                                VerbProperties rangedProperties = this.rangedComp.rangedVerb.verbProps;
                                if (rangedProperties.label == verbProperties[i].label)
                                {
                                    this.rangedComp.rangedVerbLabel = verbProperties[i].label;
                                    this.rangedComp.rangedVerbDescription = verbProperties[i].description;
                                    this.rangedComp.rangedVerbIconPath = verbProperties[i].uiIconPath;
                                    this.rangedComp.rangedVerbIconAngle = verbProperties[i].uiIconAngle;
                                    this.rangedComp.rangedVerbIconOffset = verbProperties[i].uiIconOffset;
                                }
                            }
                        }
                        yield return new FloatMenuOption(verbLabel, selectVerb);
                    }
                }
            }
        }

        public override void GizmoUpdateOnMouseover()
        {
            this.rangedComp.rangedVerb.verbProps.DrawRadiusRing(this.rangedComp.rangedVerb.caster.Position);
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            Find.Targeter.BeginTargeting(this.rangedComp.rangedVerb);
        }

        public override bool GroupsWith(Gizmo other)
        {
            return false;
        }

        public HediffComp_VerbGiverExtended rangedComp;
    }
}
