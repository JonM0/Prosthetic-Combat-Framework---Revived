using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace OrenoPCF
{
    public class Command_HediffVerbRanged : Command // TODO: maybe replace with Command_VerbTarget
    {
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach ( FloatMenuOption o in base.RightClickFloatMenuOptions )
                {
                    yield return o;
                }
                if ( this.rangedComp.AllVerbs != null ) // the verbgiver actually has verbs
                {
                    foreach ( Verb verb in this.rangedComp.AllVerbs.Where( verbs => !verbs.IsMeleeAttack ) ) // take all verbs that are not melee attacks
                    {
                        string verbLabel = verb.verbProps.label.CapitalizeFirst();

                        void selectVerb () // define what to do when you choose this verb
                        {
                            this.rangedComp.rangedVerb = verb; // set active verb

                            foreach ( PCF_VerbProperties verbProperties in this.rangedComp.Props.verbsProperties )
                            {
                                VerbProperties rangedProperties = this.rangedComp.rangedVerb.verbProps;
                                if ( rangedProperties.label == verbProperties.label )
                                {
                                    this.rangedComp.rangedVerbLabel = verbProperties.label;
                                    this.rangedComp.rangedVerbDescription = verbProperties.description;
                                    this.rangedComp.rangedVerbIconPath = verbProperties.uiIconPath;
                                    this.rangedComp.rangedVerbIconAngle = verbProperties.uiIconAngle;
                                    this.rangedComp.rangedVerbIconOffset = verbProperties.uiIconOffset;
                                }
                            }
                        }

                        yield return new FloatMenuOption( verbLabel, selectVerb );
                    }
                }
                yield break;
            }
        }

        public override void GizmoUpdateOnMouseover ()
        {
            this.rangedComp.rangedVerb.verbProps.DrawRadiusRing( this.rangedComp.rangedVerb.caster.Position );
        }

        public override void ProcessInput ( Event ev )
        {
            base.ProcessInput( ev );
            Find.Targeter.BeginTargeting( this.rangedComp.rangedVerb );
        }

        public override bool GroupsWith ( Gizmo other )
        {
            return false;
        }

        public HediffComp_VerbGiverExtended rangedComp;

        public Command_HediffVerbRanged ()
        {
            this.activateSound = SoundDefOf.Tick_Tiny;
        }
    }
}