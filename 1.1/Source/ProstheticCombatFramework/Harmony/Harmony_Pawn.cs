using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace OrenoPCF.HarmonyPatches
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
                foreach ( Hediff hediff in __instance.health.hediffSet.hediffs )
                {
                    HediffComp_HediffGizmo hediffGizmo = hediff.TryGetComp<HediffComp_HediffGizmo>();
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
