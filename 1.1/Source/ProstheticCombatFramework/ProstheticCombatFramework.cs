using System.Collections.Generic;
using System.Reflection;
using Harmony;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OrenoPCF
{
    [StaticConstructorOnStartup]
    public class ProstheticCombatFramework
    {
        static ProstheticCombatFramework()
        {
            var harmony = HarmonyInstance.Create("OrenoPCF");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public static class PCF_VanillaExtender
    {
        public static void CheckForAutoAttack(JobDriver jobDriver)
        {
            List<Hediff> hediffs = jobDriver.pawn.health.hediffSet.hediffs;
            int hediff = hediffs.Count;
            for (int i = 0; i < hediff; i++)
            {
                HediffComp_VerbGiverExtended verbGiverExtended = hediffs[i].TryGetComp<HediffComp_VerbGiverExtended>();
                if (verbGiverExtended != null)
                {
                    List<Verb> allVerbs = verbGiverExtended.AllVerbs;
                    int radVerb = Random.Range(0, allVerbs.Count);
                    if (allVerbs[radVerb] != null && verbGiverExtended.canAutoAttack && verbGiverExtended.canAttack)
                    {
                        TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat;
                        if (allVerbs[radVerb].IsIncendiary())
                        {
                            targetScanFlags |= TargetScanFlags.NeedNonBurning;
                        }
                        Thing thing = (Thing)PCF_AttackTargetFinder.BestShootTargetFromCurrentPosition(jobDriver.pawn, allVerbs[radVerb], targetScanFlags, null, 0f, 9999f);
                        if (thing != null && !allVerbs[radVerb].IsMeleeAttack)
                        {
                            verbGiverExtended.rangedVerbWarmupTime = allVerbs[radVerb].verbProps.warmupTime;
                            allVerbs[radVerb].verbProps.warmupTime = 0f;
                            allVerbs[radVerb].TryStartCastOn(thing, false, true);
                            allVerbs[radVerb].verbProps.warmupTime = verbGiverExtended.rangedVerbWarmupTime;
                        }
                    }
                    verbGiverExtended.canAttack = false;
                }
            }
        }

        public static Texture2D GetIcon(string loadID, string iconPath)
        {
            if (iconsCache.ContainsKey(loadID))
            {
                return iconsCache[loadID];
            }
            else
            {
                Texture2D icon = BaseContent.BadTex;
                if (!iconPath.NullOrEmpty())
                {
                    icon = ContentFinder<Texture2D>.Get(iconPath, true);
                    iconsCache.Add(loadID, icon);
                }
                return icon;
            }
        }

        public static void ResetIcons()
        {
            iconsCache.Clear();
        }

        public static readonly Dictionary<string, Texture2D> iconsCache = new Dictionary<string, Texture2D>();
    }
}
