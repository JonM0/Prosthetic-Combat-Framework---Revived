using HarmonyLib;
using Verse;

namespace OrenoPCF.HarmonyPatches
{
    class Harmony_BattleLogEntry_RangedImpact
    {
        [HarmonyPatch( typeof( BattleLogEntry_RangedImpact ) )]
        [HarmonyPatch( "GenerateGrammarRequest" )]
        internal class GenerateGrammarRequest
        {
            [HarmonyPrefix]
            public static bool MissingWeaponDefFix ( BattleLogEntry_RangedImpact __instance ) // Fix for the combat log not finding a projectile to put in the entry
            {
                //Log.Message( "Requested grammar" );

                var travInst = Traverse.Create( __instance );

                // extract private fields
                ThingDef weaponDef = travInst.Field( "weaponDef" ).GetValue<ThingDef>();
                ThingDef projectileDef = travInst.Field( "projectileDef" ).GetValue<ThingDef>();

                if ( weaponDef == null )
                {
                    //Log.Message( "Invalid dinfo.Weapon in TaleRecorder.RecordTale" );

                    // create a fake weapon so the battle entry can see the projectile
                    ThingDef weaponDef_fake = DefDatabase<ThingDef>.GetNamed( "PCF_LogPlaceholder" );
                    weaponDef_fake.Verbs[0].defaultProjectile = projectileDef;
                    
                    // put the new weapon in the instance
                    travInst.Field( "weaponDef" ).SetValue(weaponDef_fake); 
                }
                return true;
            }
        }
    }
}

