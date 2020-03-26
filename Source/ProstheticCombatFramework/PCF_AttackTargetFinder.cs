using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OrenoPCF
{
    public static class PCF_AttackTargetFinder
    {
        public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, Verb verb, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
        {
            Thing searcherThing = searcher.Thing;
            Pawn searcherPawn = searcher as Pawn;
            if (verb == null)
            {
                Log.Error("BestAttackTarget with " + searcher.ToStringSafe<IAttackTargetSearcher>() + " who has no attack verb.", false);
                return null;
            }
            bool onlyTargetMachines = verb.IsEMP();
            float minDistSquared = minDist * minDist;
            float num = maxTravelRadiusFromLocus + verb.verbProps.range;
            float maxLocusDistSquared = num * num;
            Func<IntVec3, bool> losValidator = null;
            if ((byte)(flags & TargetScanFlags.LOSBlockableByGas) != 0)
            {
                losValidator = delegate (IntVec3 vec3)
                {
                    Gas gas = vec3.GetGas(searcherThing.Map);
                    return gas == null || !gas.def.gas.blockTurretTracking;
                };
            }
            Predicate<IAttackTarget> innerValidator = delegate (IAttackTarget t)
            {
                Thing thing = t.Thing;
                if (t == searcher)
                {
                    return false;
                }
                if (minDistSquared > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
                {
                    return false;
                }
                if (!canTakeTargetsCloserThanEffectiveMinRange)
                {
                    float num2 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
                    if (num2 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
                    {
                        return false;
                    }
                }
                if (maxTravelRadiusFromLocus < 9999f && (float)(thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
                {
                    return false;
                }
                if (!searcherThing.HostileTo(thing))
                {
                    return false;
                }
                if (validator != null && !validator(thing))
                {
                    return false;
                }
                if (searcherPawn != null)
                {
                    Lord lord = searcherPawn.GetLord();
                    if (lord != null && !lord.LordJob.ValidateAttackTarget(searcherPawn, thing))
                    {
                        return false;
                    }
                }
                if ((byte)(flags & TargetScanFlags.NeedLOSToAll) != 0 && !searcherThing.CanSee(thing, losValidator))
                {
                    if (t is Pawn)
                    {
                        if ((byte)(flags & TargetScanFlags.NeedLOSToPawns) != 0)
                        {
                            return false;
                        }
                    }
                    else if ((byte)(flags & TargetScanFlags.NeedLOSToNonPawns) != 0)
                    {
                        return false;
                    }
                }
                if ((byte)(flags & TargetScanFlags.NeedThreat) != 0 && t.ThreatDisabled(searcher))
                {
                    return false;
                }
                if ( onlyTargetMachines && t is Pawn pawn && pawn.RaceProps.IsFlesh )
                {
                    return false;
                }
                if ((byte)(flags & TargetScanFlags.NeedNonBurning) != 0 && thing.IsBurning())
                {
                    return false;
                }
                if (searcherThing.def.race != null && searcherThing.def.race.intelligence >= Intelligence.Humanlike)
                {
                    CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
                    if (compExplosive != null && compExplosive.wickStarted)
                    {
                        return false;
                    }
                }
                if (thing.def.size.x == 1 && thing.def.size.z == 1)
                {
                    if (thing.Position.Fogged(thing.Map))
                    {
                        return false;
                    }
                }
                else
                {
                    bool flag2 = false;
                    foreach ( IntVec3 cellRect in thing.OccupiedRect() )
                    {
                        if ( cellRect.Fogged(thing.Map) )
                        {
                            flag2 = true;
                            break;
                        }
                    }
                    if (!flag2)
                    {
                        return false;
                    }
                }
                return true;
            };
            if (PCF_AttackTargetFinder.HasRangedAttack(searcher, verb))
            {
                PCF_AttackTargetFinder.tmpTargets.Clear();
                PCF_AttackTargetFinder.tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
                if ((byte)(flags & TargetScanFlags.NeedReachable) != 0)
                {
                    Predicate<IAttackTarget> oldValidator = innerValidator;
                    innerValidator = ((IAttackTarget t) => oldValidator(t) && PCF_AttackTargetFinder.CanReach(searcherThing, t.Thing, canBash));
                }
                bool flag = false;
                for (int i = 0; i < PCF_AttackTargetFinder.tmpTargets.Count; i++)
                {
                    IAttackTarget attackTarget = PCF_AttackTargetFinder.tmpTargets[i];
                    if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && innerValidator(attackTarget) && PCF_AttackTargetFinder.CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
                    {
                        flag = true;
                        break;
                    }
                }
                IAttackTarget result;
                if (flag)
                {
                    PCF_AttackTargetFinder.tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
                    result = PCF_AttackTargetFinder.GetRandomShootingTargetByScore(PCF_AttackTargetFinder.tmpTargets, searcher, verb);
                }
                else
                {
                    Predicate<Thing> validator2;
                    if ((byte)(flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != 0 && (byte)(flags & TargetScanFlags.NeedReachable) == 0)
                    {
                        validator2 = ((Thing t) => innerValidator((IAttackTarget)t) && (PCF_AttackTargetFinder.CanReach(searcherThing, t, canBash) || PCF_AttackTargetFinder.CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb)));
                    }
                    else
                    {
                        validator2 = ((Thing t) => innerValidator((IAttackTarget)t));
                    }
                    result = (IAttackTarget)GenClosest.ClosestThing_Global(searcherThing.Position, PCF_AttackTargetFinder.tmpTargets, maxDist, validator2, null);
                }
                PCF_AttackTargetFinder.tmpTargets.Clear();
                return result;
            }
            if (searcherPawn != null && searcherPawn.mindState.duty != null && searcherPawn.mindState.duty.radius > 0f && !searcherPawn.InMentalState)
            {
                Predicate<IAttackTarget> oldValidator = innerValidator;
                innerValidator = ((IAttackTarget t) => oldValidator(t) && t.Thing.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell, searcherPawn.mindState.duty.radius));
            }
            IntVec3 position = searcherThing.Position;
            Map map = searcherThing.Map;
            ThingRequest thingReq = ThingRequest.ForGroup(ThingRequestGroup.AttackTarget);
            PathEndMode peMode = PathEndMode.Touch;
            Pawn searcherPawn2 = searcherPawn;
            Danger maxDanger = Danger.Deadly;
            bool canBash2 = canBash;
            TraverseParms traverseParams = TraverseParms.For(searcherPawn2, maxDanger, TraverseMode.ByPawn, canBash2);
            float maxDist2 = maxDist;
            bool validator3(Thing x) => innerValidator((IAttackTarget)x);
            int searchRegionsMax = (maxDist <= 800f) ? 40 : -1;
            IAttackTarget attackTarget2 = (IAttackTarget)GenClosest.ClosestThingReachable(position, map, thingReq, peMode, traverseParams, maxDist2, validator3, null, 0, searchRegionsMax, false, RegionType.Set_Passable, false);
            if (attackTarget2 != null && PawnUtility.ShouldCollideWithPawns(searcherPawn))
            {
                IAttackTarget attackTarget3 = PCF_AttackTargetFinder.FindBestReachableMeleeTarget(innerValidator, searcherPawn, maxDist, canBash);
                if (attackTarget3 != null)
                {
                    float lengthHorizontal = (searcherPawn.Position - attackTarget2.Thing.Position).LengthHorizontal;
                    float lengthHorizontal2 = (searcherPawn.Position - attackTarget3.Thing.Position).LengthHorizontal;
                    if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
                    {
                        attackTarget2 = attackTarget3;
                    }
                }
            }
            return attackTarget2;
        }

        private static bool CanReach(Thing searcher, Thing target, bool canBash)
        {
            if (searcher is Pawn pawn)
            {
                if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Some, canBash, TraverseMode.ByPawn))
                {
                    return false;
                }
            }
            else
            {
                TraverseMode mode = (!canBash) ? TraverseMode.NoPassClosedDoors : TraverseMode.PassDoors;
                if (!searcher.Map.reachability.CanReach(searcher.Position, target, PathEndMode.Touch, TraverseParms.For(mode, Danger.Deadly, false)))
                {
                    return false;
                }
            }
            return true;
        }

        private static IAttackTarget FindBestReachableMeleeTarget(Predicate<IAttackTarget> validator, Pawn searcherPawn, float maxTargDist, bool canBash)
        {
            maxTargDist = Mathf.Min(maxTargDist, 30f);
            IAttackTarget reachableTarget = null;
            IAttackTarget bestTargetOnCell(IntVec3 x)
            {
                List<Thing> thingList = x.GetThingList(searcherPawn.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing is IAttackTarget attackTarget)
                    {
                        if (validator(attackTarget))
                        {
                            if (ReachabilityImmediate.CanReachImmediate(x, thing, searcherPawn.Map, PathEndMode.Touch, searcherPawn))
                            {
                                if (searcherPawn.CanReachImmediate(thing, PathEndMode.Touch) || searcherPawn.Map.attackTargetReservationManager.CanReserve(searcherPawn, attackTarget))
                                {
                                    return attackTarget;
                                }
                            }
                        }
                    }
                }
                return null;
            }
            searcherPawn.Map.floodFiller.FloodFill(searcherPawn.Position, delegate (IntVec3 x)
            {
                if (!x.Walkable(searcherPawn.Map))
                {
                    return false;
                }
                if ((float)x.DistanceToSquared(searcherPawn.Position) > maxTargDist * maxTargDist)
                {
                    return false;
                }
                if (!canBash)
                {
                    if (x.GetEdifice(searcherPawn.Map) is Building_Door building_Door && !building_Door.CanPhysicallyPass(searcherPawn))
                    {
                        return false;
                    }
                }
                return !PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, true, false, false);
            }, delegate (IntVec3 x)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 intVec = x + GenAdj.AdjacentCells[i];
                    if (intVec.InBounds(searcherPawn.Map))
                    {
                        IAttackTarget attackTarget = bestTargetOnCell(intVec);
                        if (attackTarget != null)
                        {
                            reachableTarget = attackTarget;
                            break;
                        }
                    }
                }
                return reachableTarget != null;
            }, int.MaxValue, false, null);
            return reachableTarget;
        }

        private static bool HasRangedAttack(IAttackTargetSearcher t, Verb verb)
        {
            return verb != null && !verb.verbProps.IsMeleeAttack;
        }

        private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            return verb != null && verb.CanHitTargetFrom(searcher.Thing.Position, target.Thing);
        }

        private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, IAttackTargetSearcher searcher, Verb verb)
        {
            if (PCF_AttackTargetFinder.GetAvailableShootingTargetsByScore(targets, searcher, verb).TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out Pair<IAttackTarget, float> pair))
            {
                return pair.First;
            }
            return null;
        }

        private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb)
        {
            PCF_AttackTargetFinder.availableShootingTargets.Clear();
            if (rawTargets.Count == 0)
            {
                return PCF_AttackTargetFinder.availableShootingTargets;
            }
            PCF_AttackTargetFinder.tmpTargetScores.Clear();
            PCF_AttackTargetFinder.tmpCanShootAtTarget.Clear();
            float num = 0f;
            IAttackTarget attackTarget = null;
            for (int i = 0; i < rawTargets.Count; i++)
            {
                PCF_AttackTargetFinder.tmpTargetScores.Add(float.MinValue);
                PCF_AttackTargetFinder.tmpCanShootAtTarget.Add(false);
                if (rawTargets[i] != searcher)
                {
                    bool flag = PCF_AttackTargetFinder.CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
                    PCF_AttackTargetFinder.tmpCanShootAtTarget[i] = flag;
                    if (flag)
                    {
                        float shootingTargetScore = PCF_AttackTargetFinder.GetShootingTargetScore(rawTargets[i], searcher, verb);
                        PCF_AttackTargetFinder.tmpTargetScores[i] = shootingTargetScore;
                        if (attackTarget == null || shootingTargetScore > num)
                        {
                            attackTarget = rawTargets[i];
                            num = shootingTargetScore;
                        }
                    }
                }
            }
            if (num < 1f)
            {
                if (attackTarget != null)
                {
                    PCF_AttackTargetFinder.availableShootingTargets.Add(new Pair<IAttackTarget, float>(attackTarget, 1f));
                }
            }
            else
            {
                float num2 = num - 30f;
                for (int j = 0; j < rawTargets.Count; j++)
                {
                    if (rawTargets[j] != searcher)
                    {
                        if (PCF_AttackTargetFinder.tmpCanShootAtTarget[j])
                        {
                            float num3 = PCF_AttackTargetFinder.tmpTargetScores[j];
                            if (num3 >= num2)
                            {
                                float second = Mathf.InverseLerp(num - 30f, num, num3);
                                PCF_AttackTargetFinder.availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], second));
                            }
                        }
                    }
                }
            }
            return PCF_AttackTargetFinder.availableShootingTargets;
        }

        private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            float num = 60f;
            num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
            if (target.TargetCurrentlyAimingAt == searcher.Thing)
            {
                num += 10f;
            }
            if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
            {
                num += 40f;
            }
            num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
            if (target is Pawn pawn && pawn.RaceProps.Animal && pawn.Faction != null && !pawn.IsFighting())
            {
                num -= 50f;
            }
            num += PCF_AttackTargetFinder.FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
            return num + PCF_AttackTargetFinder.FriendlyFireConeTargetScoreOffset(target, searcher, verb);
        }

        private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
            {
                return 0f;
            }
            Map map = target.Thing.Map;
            IntVec3 position = target.Thing.Position;
            int num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
            float num2 = 0f;
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map))
                {
                    bool flag = true;
                    List<Thing> thingList = intVec.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] is IAttackTarget && thingList[j] != target)
                        {
                            if (flag)
                            {
                                if (!GenSight.LineOfSight(position, intVec, map, true, null, 0, 0))
                                {
                                    break;
                                }
                                flag = false;
                            }
                            float num3;
                            if (thingList[j] == searcher)
                            {
                                num3 = 40f;
                            }
                            else if (thingList[j] is Pawn)
                            {
                                num3 = ((!thingList[j].def.race.Animal) ? 18f : 7f);
                            }
                            else
                            {
                                num3 = 10f;
                            }
                            if (searcher.Thing.HostileTo(thingList[j]))
                            {
                                num2 += num3 * 0.6f;
                            }
                            else
                            {
                                num2 -= num3;
                            }
                        }
                    }
                }
            }
            return num2;
        }

        private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            if (!(searcher.Thing is Pawn pawn))
            {
                return 0f;
            }
            if (pawn.RaceProps.intelligence < Intelligence.ToolUser)
            {
                return 0f;
            }
            if (pawn.RaceProps.IsMechanoid)
            {
                return 0f;
            }
            if (!(verb is Verb_Shoot verb_Shoot))
            {
                return 0f;
            }
            ThingDef defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
            if (defaultProjectile == null)
            {
                return 0f;
            }
            if (defaultProjectile.projectile.flyOverhead)
            {
                return 0f;
            }
            Map map = pawn.Map;
            ShotReport report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
            float a = VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.forcedMissRadius, report.ShootLine.Dest - report.ShootLine.Source);
            float radius = Mathf.Max(a, 1.5f);
            IntVec3 dest2 = report.ShootLine.Dest;
            IEnumerable<IntVec3> source = from dest in GenRadial.RadialCellsAround(dest2, radius, true) where dest.InBounds(map) select dest;
            IEnumerable<ShootLine> source2 = from dest in source select new ShootLine(report.ShootLine.Source, dest);
            IEnumerable<IntVec3> source3 = source2.SelectMany((ShootLine line) => line.Points().Concat(line.Dest).TakeWhile((IntVec3 pos) => pos.CanBeSeenOverFast(map)));
            IEnumerable<IntVec3> enumerable = source3.Distinct<IntVec3>();
            float num = 0f;
            foreach (IntVec3 c in enumerable)
            {
                float num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), c);
                if (num2 > 0f)
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing thing = thingList[i];
                        if (thing is IAttackTarget && thing != target)
                        {
                            float num3;
                            if (thing == searcher)
                            {
                                num3 = 40f;
                            }
                            else if (thing is Pawn)
                            {
                                num3 = ((!thing.def.race.Animal) ? 18f : 7f);
                            }
                            else
                            {
                                num3 = 10f;
                            }
                            num3 *= num2;
                            if (searcher.Thing.HostileTo(thing))
                            {
                                num3 *= 0.6f;
                            }
                            else
                            {
                                num3 *= -1f;
                            }
                            num += num3;
                        }
                    }
                }
            }
            return num;
        }

        public static IAttackTarget BestShootTargetFromCurrentPosition(IAttackTargetSearcher searcher, Verb verb, TargetScanFlags flags, Predicate<Thing> validator = null, float minDistance = 0f, float maxDistance = 9999f)
        {
            if (verb == null)
            {
                Log.Error("BestShootTargetFromCurrentPosition with " + searcher.ToStringSafe<IAttackTargetSearcher>() + " who has no attack verb.", false);
                return null;
            }
            return PCF_AttackTargetFinder.BestAttackTarget(searcher, verb, flags, validator, Mathf.Max(minDistance, verb.verbProps.minRange), Mathf.Min(maxDistance, verb.verbProps.range), default(IntVec3), float.MaxValue, false, false);
        }

        private static List<IAttackTarget> tmpTargets = new List<IAttackTarget>();

        private static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();

        private static List<float> tmpTargetScores = new List<float>();

        private static List<bool> tmpCanShootAtTarget = new List<bool>();
    }
}
