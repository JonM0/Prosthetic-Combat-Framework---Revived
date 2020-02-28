using Harmony;
using RimWorld;
using Verse.AI;

namespace OrenoPCF.Harmony
{
    public class Harmony_JobDriverWait
    {
        [HarmonyPatch(typeof(JobDriver_Wait))]
        [HarmonyPatch("CheckForAutoAttack")]
        internal class JobDriverWait_CheckForAutoAttack
        {
            [HarmonyPostfix]
            private static void VerbGiverExtended(JobDriver_Wait __instance)
            {
                if (__instance.pawn.Downed)
                {
                    return;
                }
                if (__instance.pawn.Faction != null && __instance.job.def == JobDefOf.Wait_Combat && (__instance.pawn.drafter == null || __instance.pawn.drafter.FireAtWill))
                {
                    PCF_VanillaExtender.CheckForAutoAttack(__instance);
                }
            }
        }
    }
}
