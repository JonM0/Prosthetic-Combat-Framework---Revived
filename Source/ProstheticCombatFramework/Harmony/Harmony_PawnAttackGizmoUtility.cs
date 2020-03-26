﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace OrenoPCF.HarmonyPatches
{
    public class Harmony_PawnAttackGizmoUtility
    {
        [HarmonyPatch(typeof(PawnAttackGizmoUtility))]
        [HarmonyPatch("GetAttackGizmos")]
        internal class PawnAttackGizmoUtility_GetAttackGizmos
        {
            [HarmonyPostfix]
            private static void HediffVerbGiverExtended(ref IEnumerable<Gizmo> __result, Pawn pawn)
            {
                List<Gizmo> gizmos = new List<Gizmo>(__result);
                if (pawn.Drafted)
                {
                    foreach ( Hediff hediff in pawn.health.hediffSet.hediffs )
                    {
                        HediffComp_VerbGiverExtended verbGiverExtended = hediff.TryGetComp<HediffComp_VerbGiverExtended>();
                        if (verbGiverExtended != null)
                        {
                            foreach (Verb meleeVerb in verbGiverExtended.AllVerbs.Where(verbs => verbs.IsMeleeAttack)) // for each melee verb added by a hediff
                            {
                                foreach (PCF_VerbProperties verbProperties in verbGiverExtended.Props.verbsProperties)
                                {
                                    if (meleeVerb.verbProps.label == verbProperties.label)
                                    {
                                        Command_HediffVerbMelee command_HediffVerbMelee = new Command_HediffVerbMelee
                                        {
                                            verb = meleeVerb,
                                            defaultLabel = verbProperties.label,
                                            defaultDesc = verbProperties.description.CapitalizeFirst(),
                                            icon = PCF_VanillaExtender.GetIcon(verbGiverExtended.Pawn.GetUniqueLoadID() + "_" + meleeVerb.loadID, verbProperties.uiIconPath),
                                            iconAngle = verbProperties.uiIconAngle,
                                            iconOffset = verbProperties.uiIconOffset
                                        };
                                        if (pawn.Faction != Faction.OfPlayer)
                                        {
                                            command_HediffVerbMelee.Disable("CannotOrderNonControlledLower".Translate());
                                        }
                                        else if (pawn.IsColonist)
                                        {
                                            if (pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
                                            {
                                                command_HediffVerbMelee.Disable("IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
                                            }
                                        }
                                        gizmos.Add(command_HediffVerbMelee);
                                    }
                                }
                            }
                        }
                    }
                }
                __result = gizmos;
            }
        }
    }
}
