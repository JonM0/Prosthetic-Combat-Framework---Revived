using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace OrenoPCF
{
    public class HediffComp_VerbGiverExtended : HediffComp, IVerbOwner
    {
        public HediffComp_VerbGiverExtended()
        {
            this.verbTracker = new VerbTracker(this);
        }

        public HediffCompProperties_VerbGiverExtended Props
        {
            get
            {
                return (HediffCompProperties_VerbGiverExtended)this.props;
            }
        }

        public List<Verb> AllVerbs
        {
            get
            {
                return this.verbTracker.AllVerbs;
            }
        }

        public VerbTracker VerbTracker
        {
            get
            {
                return this.verbTracker;
            }
        }

        public List<VerbProperties> VerbProperties
        {
            get
            {
                return this.Props.verbs;
            }
        }

        public List<Tool> Tools
        {
            get
            {
                return null;
            }
        }

        Thing IVerbOwner.ConstantCaster
        {
            get
            {
                return base.Pawn;
            }
        }

        ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef
        {
            get
            {
                return ImplementOwnerTypeDefOf.Hediff;
            }
        }

        public void InitializeRangedVerb()
        {
            this.rangedVerb = this.AllVerbs.Where(verbs => !verbs.IsMeleeAttack).FirstOrDefault();
            foreach ( PCF_VerbProperties verbProperty in this.Props.verbsProperties )
            {
                VerbProperties rangedProperties = this.rangedVerb.verbProps;
                if (rangedProperties.label == verbProperty.label)
                {
                    this.rangedVerbLabel = verbProperty.label;
                    this.rangedVerbDescription = verbProperty.description;
                    this.rangedVerbIconPath = verbProperty.uiIconPath;
                    this.rangedVerbIconAngle = verbProperty.uiIconAngle;
                    this.rangedVerbIconOffset = verbProperty.uiIconOffset;
                }
            }
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            this.InitializeRangedVerb();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            Scribe_Deep.Look<VerbTracker>(ref this.verbTracker, "verbTracker", new object[]
            {
                this
            });
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.verbTracker == null)
            {
                this.verbTracker = new VerbTracker(this);
            }

            Scribe_Values.Look<bool>(ref this.canAutoAttack, "canAutoAttack", true, false);

            //Scribe_Deep.Look<Verb>(ref this.rangedVerb, "rangedVerb", null, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (this.rangedVerb == null || this.rangedVerbLabel == null))
            {
                this.InitializeRangedVerb();
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            this.verbTracker.VerbsTick();

            if (this.autoAttackTick < Find.TickManager.TicksGame)
            {
                this.canAttack = true;
                this.autoAttackTick = Find.TickManager.TicksGame + (int)Rand.Range(0.8f * this.autoAttackFrequency, 1.2f * this.autoAttackFrequency);
            }
        }

        //public override void Notify_PawnUsedVerb ( Verb verb, LocalTargetInfo target )  // DEBUG PURPOSES    ---------
        //{
        //    Log.Message( "Notify_PawnUsedVerb was called: " + verb.ToString() );
        //}

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            PCF_VanillaExtender.ResetIcons();
        }

        string IVerbOwner.UniqueVerbOwnerID()
        {
            return this.parent.GetUniqueLoadID() + "_" + this.parent.comps.IndexOf(this);
        }

        bool IVerbOwner.VerbsStillUsableBy(Pawn p)
        {
            return p.health.hediffSet.hediffs.Contains(this.parent);
        }

        public VerbTracker verbTracker;

        /* ===  Manual Ranged Verb  === */
        public Verb rangedVerb;

        public string rangedVerbLabel;

        public string rangedVerbDescription;

        public string rangedVerbIconPath;

        public float rangedVerbIconAngle;

        public Vector2 rangedVerbIconOffset;

        public float rangedVerbWarmupTime;
        /* ==========  END  =========== */

        public bool canAttack = false;

        public bool canAutoAttack = true;

        private int autoAttackTick = 0;

        private readonly int autoAttackFrequency = 100;
    }
}
