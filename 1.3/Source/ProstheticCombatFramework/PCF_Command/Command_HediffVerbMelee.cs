//using UnityEngine;
//using RimWorld;
//using Verse;
//using Verse.Sound;

//namespace OrenoPCF
//{
//    public class Command_HediffVerbMelee : Command
//    {
//        public override void ProcessInput(Event ev)
//        {
//            base.ProcessInput(ev);
//            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
//            Find.Targeter.BeginTargeting(this.verb);
//        }

//        public override bool GroupsWith(Gizmo other)
//        {
//            return false;
//        }

//        public override bool InheritInteractionsFrom(Gizmo other)
//        {
//            return false;
//        }

//        public Verb verb;
//    }
//}