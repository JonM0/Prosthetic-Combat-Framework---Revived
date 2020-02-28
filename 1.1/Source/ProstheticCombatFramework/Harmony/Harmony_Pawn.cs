using System.Collections.Generic;
using Harmony;
using Verse;

namespace OrenoPCF.Harmony
{
    public class Harmony_Pawn
    {
        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("GetGizmos")]
        internal class Pawn_GetGizmos
        {
            [HarmonyPostfix]
            public static void HediffGizmos(ref IEnumerable<Gizmo> __result, Pawn __instance)
            {
                List<Gizmo> gizmos = new List<Gizmo>(__result);
                List<Hediff> hediffs = __instance.health.hediffSet.hediffs;
                int hediff = hediffs.Count;
                for (int i = 0; i < hediff; i++)
                {
                    HediffComp_HediffGizmo hediffGizmo = hediffs[i].TryGetComp<HediffComp_HediffGizmo>();
                    if (hediffGizmo != null)
                    {
                        foreach (Gizmo h in hediffGizmo.CompGetGizmos())
                        {
                            gizmos.Add(h);
                        }
                    }
                }
                __result = gizmos;
            }
        }
    }
}
