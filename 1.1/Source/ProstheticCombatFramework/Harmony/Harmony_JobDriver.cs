using System.Collections.Generic;
using System.Linq;
using Harmony;
using Verse;
using Verse.AI;

namespace OrenoPCF.Harmony
{
    public class Harmony_JobDriver
    {
        [HarmonyPatch(typeof(JobDriver))]
        [HarmonyPatch("SetupToils")]
        internal class JobDriver_SetupToils
        {
            [HarmonyPostfix]
            private static void VerbGiverExtended(JobDriver __instance)
            {
                /*
                // Patch inspirated from RunAndGun mod by roolo
                // License for patch when i make this mod "10 July 2019"
                // That patch is free to use except don't make exact copies or only with minor adjustment of Run And Gun. The author also give permission.
                // --- github.com/rheirman/RunAndGun/blob/master/README.md ---
                */
                if (__instance is JobDriver_Goto jobDriver_Goto)
                {
                    List<Toil> value = Traverse.Create(jobDriver_Goto).Field("toils").GetValue<List<Toil>>();
                    if (value.Count() > 0)
                    {
                        Toil toil = value.ElementAt(0);
                        toil.AddPreTickAction(delegate
                        {
                            if (jobDriver_Goto.pawn.Downed)
                            {
                                return;
                            }
                            if (jobDriver_Goto.pawn.Faction != null && (jobDriver_Goto.pawn.drafter == null || jobDriver_Goto.pawn.drafter.FireAtWill) && jobDriver_Goto.pawn.IsHashIntervalTick(10))
                            {
                                PCF_VanillaExtender.CheckForAutoAttack(jobDriver_Goto);
                            }
                        });
                    }
                }
            }
        }
    }
}
