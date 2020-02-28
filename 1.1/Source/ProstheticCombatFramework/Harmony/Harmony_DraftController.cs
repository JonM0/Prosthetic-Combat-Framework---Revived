using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace OrenoPCF.Harmony
{
    public class Harmony_DraftController
    {
        [HarmonyPatch(typeof(Pawn_DraftController))]
        [HarmonyPatch("GetGizmos")]
        internal class DraftController_GetGizmos
        {
            [HarmonyPostfix]
            private static void VerbGiverExtended(ref IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
            {
                List<Gizmo> gizmos = new List<Gizmo>(__result);
                List<Gizmo> toggleGizmos = new List<Gizmo>();
                List<Gizmo> rangedVerbGizmos = new List<Gizmo>();

                List<Hediff> hediffs = __instance.pawn.health.hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    HediffComp_VerbGiverExtended verbGiverExtended = hediffs[i].TryGetComp<HediffComp_VerbGiverExtended>();
                    if (verbGiverExtended != null)
                    {
                        Command_HediffToggle command_HediffToggle = new Command_HediffToggle
                        {
                            isActive = (() => verbGiverExtended.canAutoAttack),
                            toggleAction = delegate ()
                            {
                                verbGiverExtended.canAutoAttack = !verbGiverExtended.canAutoAttack;
                            },
                            defaultLabel = verbGiverExtended.Props.toggleLabel,
                            defaultDesc = verbGiverExtended.Props.toggleDescription.CapitalizeFirst(),
                            icon = PCF_VanillaExtender.GetIcon(verbGiverExtended.Pawn.GetUniqueLoadID() + "_" + verbGiverExtended.parent.GetUniqueLoadID(), verbGiverExtended.Props.toggleIconPath),
                            iconAngle = verbGiverExtended.Props.toggleIconAngle,
                            iconOffset = verbGiverExtended.Props.toggleIconOffset
                        };
                        if (__instance.pawn.Faction != Faction.OfPlayer)
                        {
                            command_HediffToggle.Disable("CannotOrderNonControlled".Translate());
                        }
                        if (__instance.pawn.Downed)
                        {
                            command_HediffToggle.Disable("IsIncapped".Translate(__instance.pawn.LabelShort, __instance.pawn));
                        }
                        toggleGizmos.Add(command_HediffToggle);

                        Command_HediffVerbRanged command_HediffVerbRanged = new Command_HediffVerbRanged
                        {
                            rangedComp = verbGiverExtended,
                            defaultLabel = verbGiverExtended.rangedVerbLabel,
                            defaultDesc = verbGiverExtended.rangedVerbDescription.CapitalizeFirst(),
                            icon = PCF_VanillaExtender.GetIcon(verbGiverExtended.Pawn.GetUniqueLoadID() + "_" + verbGiverExtended.rangedVerb.loadID, verbGiverExtended.rangedVerbIconPath),
                            iconAngle = verbGiverExtended.rangedVerbIconAngle,
                            iconOffset = verbGiverExtended.rangedVerbIconOffset
                        };
                        if (__instance.pawn.Faction != Faction.OfPlayer)
                        {
                            command_HediffVerbRanged.Disable("CannotOrderNonControlled".Translate());
                        }
                        else if (__instance.pawn.IsColonist)
                        {
                            if (__instance.pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                            {
                                command_HediffVerbRanged.Disable("IsIncapableOfViolence".Translate(__instance.pawn.LabelShort, __instance.pawn));
                            }
                            else if (!__instance.pawn.drafter.Drafted)
                            {
                                command_HediffVerbRanged.Disable("IsNotDrafted".Translate(__instance.pawn.LabelShort, __instance.pawn));
                            }
                        }
                        rangedVerbGizmos.Add(command_HediffVerbRanged);
                    }
                }

                __result = gizmos.Concat(toggleGizmos).Concat(rangedVerbGizmos);
            }
        }
    }
}
