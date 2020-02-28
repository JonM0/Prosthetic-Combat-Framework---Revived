using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace OrenoPCF
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
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                    for (int i = 0; i < hediffs.Count; i++)
                    {
                        HediffComp_VerbGiverExtended verbGiverExtended = hediffs[i].TryGetComp<HediffComp_VerbGiverExtended>();
                        if (verbGiverExtended != null)
                        {
                            foreach (Verb verb in verbGiverExtended.AllVerbs.Where(verbs => verbs.IsMeleeAttack))
                            {
                                foreach (PCF_VerbProperties verbProperties in verbGiverExtended.Props.verbsProperties)
                                {
                                    if (verb.verbProps.label == verbProperties.label)
                                    {
                                        Command_HediffVerbMelee command_HediffVerbMelee = new Command_HediffVerbMelee
                                        {
                                            verb = verb,
                                            defaultLabel = verbProperties.label,
                                            defaultDesc = verbProperties.description.CapitalizeFirst(),
                                            icon = PCF_VanillaExtender.GetIcon(verbGiverExtended.Pawn.GetUniqueLoadID() + "_" + verb.loadID, verbProperties.uiIconPath),
                                            iconAngle = verbProperties.uiIconAngle,
                                            iconOffset = verbProperties.uiIconOffset
                                        };
                                        if (pawn.Faction != Faction.OfPlayer)
                                        {
                                            command_HediffVerbMelee.Disable("CannotOrderNonControlledLower".Translate());
                                        }
                                        else if (pawn.IsColonist)
                                        {
                                            if (pawn.story.WorkTagIsDisabled(WorkTags.Violent))
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
