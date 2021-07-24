using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrenoPCF
{
    public class JobDriver_AttackStaticExtended : JobDriver
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.startedIncapacitated, "startedIncapacitated", false, false);
            Scribe_Values.Look<int>(ref this.numAttacksMade, "numAttacksMade", 0, false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    if (base.TargetThingA is Pawn pawn)
                    {
                        this.startedIncapacitated = pawn.Downed;
                    }
                    this.pawn.pather.StopDead();
                },
                tickAction = delegate ()
                {
                    if (!base.TargetA.IsValid)
                    {
                        base.EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                    if (base.TargetA.HasThing)
                    {
                        Pawn pawn = base.TargetA.Thing as Pawn;
                        if (base.TargetA.Thing.Destroyed || (pawn != null && !this.startedIncapacitated && pawn.Downed))
                        {
                            base.EndJobWith(JobCondition.Succeeded);
                            return;
                        }
                    }
                    if (this.numAttacksMade >= this.job.maxNumStaticAttacks && !this.pawn.stances.FullBodyBusy)
                    {
                        base.EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                    if (this.TryStartAttack(base.TargetA))
                    {
                        this.numAttacksMade++;
                    }
                    else if (!this.pawn.stances.FullBodyBusy)
                    {
                        Verb verb = this.job.verbToUse;
                        if (this.job.endIfCantShootTargetFromCurPos && (verb == null || !verb.CanHitTargetFrom(this.pawn.Position, base.TargetA)))
                        {
                            base.EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                        if (this.job.endIfCantShootInMelee)
                        {
                            if (verb == null)
                            {
                                base.EndJobWith(JobCondition.Incompletable);
                                return;
                            }
                            float num = verb.verbProps.EffectiveMinRange(base.TargetA, this.pawn);
                            if ((float)this.pawn.Position.DistanceToSquared(base.TargetA.Cell) < num * num && this.pawn.Position.AdjacentTo8WayOrInside(base.TargetA.Cell))
                            {
                                base.EndJobWith(JobCondition.Incompletable);
                                return;
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            yield break;
        }

        public bool TryStartAttack(LocalTargetInfo targ)
        {
            if (pawn.stances.FullBodyBusy)
            {
                return false;
            }
            if (pawn.story != null && pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag( WorkTags.Violent ) )
            {
                return false;
            }
            bool allowManualCastWeapons = !pawn.IsColonist;
            Verb verb = this.job.verbToUse;
            return verb != null && verb.TryStartCastOn(targ, false, true);
        }

        private bool startedIncapacitated;

        private int numAttacksMade;
    }
}
