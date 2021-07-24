using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrenoPCF.HarmonyPatches
{
    public class Harmony_Targeter
    {
        [HarmonyPatch( typeof( Targeter ) )]
        [HarmonyPatch( "OrderPawnForceTarget" )]
        internal class Targeter_CurrentTargetUnderMouse
        {
            [HarmonyPrefix]
            public static bool VerbGiverExtended ( Targeter __instance, ITargetingSource targetingSource )
            {
                Verb verb = targetingSource.GetVerb;
                if ( verb == null )
                {
                    return true;
                }
                if ( verb.EquipmentSource != null || verb.EquipmentCompSource != null )
                {
                    return true;
                }
                if ( verb.HediffSource != null || verb.HediffCompSource != null )
                {
                    return true;
                }
                if ( verb.TerrainSource != null || verb.TerrainDefSource != null )
                {
                    return true;
                }
                if ( verb is Verb_CastPsycast )
                {
                    return true;
                }

                if ( verb.verbProps.IsMeleeAttack )
                {
                    Traverse.Create( __instance ).Field( "targetParams" ).SetValue( TargetingParameters.ForAttackAny() );
                }
                LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse( __instance, true );
                if ( !localTargetInfo.IsValid )
                {
                    return true;
                }
                if ( verb.CasterPawn != localTargetInfo.Thing )
                {
                    if ( verb.verbProps.IsMeleeAttack )
                    {
                        Job job = new Job( JobDefOf.AttackMelee, localTargetInfo )
                        {
                            verbToUse = verb,
                            playerForced = true
                        };
                        if ( localTargetInfo.Thing is Pawn pawn )
                        {
                            job.killIncappedTarget = pawn.Downed;
                        }
                        verb.CasterPawn.jobs.TryTakeOrderedJob( job, JobTag.Misc );
                    }
                    else
                    {
                        float num = verb.verbProps.EffectiveMinRange( localTargetInfo, verb.CasterPawn );
                        if ( (float)verb.CasterPawn.Position.DistanceToSquared( localTargetInfo.Cell ) < num * num && verb.CasterPawn.Position.AdjacentTo8WayOrInside( localTargetInfo.Cell ) )
                        {
                            Messages.Message( "MessageCantShootInMelee".Translate(), verb.CasterPawn, MessageTypeDefOf.RejectInput, false );
                        }
                        else
                        {
                            JobDef def = (!verb.verbProps.ai_IsWeapon) ? JobDefOf.UseVerbOnThing : PCF_JobDefOf.PCF_AttackStaticExtended;
                            Job job2 = new Job( def )
                            {
                                verbToUse = verb,
                                targetA = localTargetInfo,
                                endIfCantShootInMelee = true
                            };
                            verb.CasterPawn.jobs.TryTakeOrderedJob( job2, JobTag.Misc );
                        }
                    }
                }
                return false;
            }

            private static LocalTargetInfo CurrentTargetUnderMouse ( Targeter targeter, bool mustBeHittableNowIfNotMelee )
            {
                if ( !targeter.IsTargeting )
                {
                    return LocalTargetInfo.Invalid;
                }
                var targetParams = Traverse.Create( targeter ).Field( "targetParams" ).GetValue<TargetingParameters>();
                TargetingParameters clickParams = (targeter.targetingSource == null) ? targetParams : targeter.targetingSource.GetVerb.verbProps.targetParams;
                LocalTargetInfo localTargetInfo = LocalTargetInfo.Invalid;
                using ( IEnumerator<LocalTargetInfo> enumerator = GenUI.TargetsAtMouse( clickParams, false ).GetEnumerator() )
                {
                    if ( enumerator.MoveNext() )
                    {
                        LocalTargetInfo localTargetInfo2 = enumerator.Current;
                        localTargetInfo = localTargetInfo2;
                    }
                }
                if ( localTargetInfo.IsValid && mustBeHittableNowIfNotMelee && !(localTargetInfo.Thing is Pawn) && targeter.targetingSource != null && !targeter.targetingSource.GetVerb.verbProps.IsMeleeAttack )
                {
                    if ( targeter.targetingSourceAdditionalPawns != null && targeter.targetingSourceAdditionalPawns.Any<Pawn>() )
                    {
                        bool flag = false;
                        for ( int i = 0; i < targeter.targetingSourceAdditionalPawns.Count; i++ )
                        {
                            Verb verb = GetTargetingVerb( targeter, targeter.targetingSourceAdditionalPawns[i] );
                            if ( verb != null && verb.CanHitTarget( localTargetInfo ) )
                            {
                                flag = true;
                                break;
                            }
                        }
                        if ( !flag )
                        {
                            localTargetInfo = LocalTargetInfo.Invalid;
                        }
                    }
                    else if ( !targeter.targetingSource.CanHitTarget( localTargetInfo ) )
                    {
                        localTargetInfo = LocalTargetInfo.Invalid;
                    }
                }
                return localTargetInfo;
            }

            private static Verb GetTargetingVerb ( Targeter targeter, Pawn pawn )
            {
                return pawn.equipment.AllEquipmentVerbs.FirstOrDefault( ( Verb x ) => x.verbProps == targeter.targetingSource.GetVerb.verbProps && !(x is Verb_CastPsycast) );
            }
        }
    }
}