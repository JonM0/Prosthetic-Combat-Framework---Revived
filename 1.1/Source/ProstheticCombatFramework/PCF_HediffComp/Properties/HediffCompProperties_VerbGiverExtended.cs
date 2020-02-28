using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OrenoPCF
{
    public class HediffCompProperties_VerbGiverExtended : HediffCompProperties
    {
        public HediffCompProperties_VerbGiverExtended()
        {
            this.compClass = typeof(HediffComp_VerbGiverExtended);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string err in base.ConfigErrors(parentDef))
            {
                yield return err;
            }
            if (this.verbs != null)
            {
                VerbProperties dupeVerb = this.verbs.SelectMany((VerbProperties lhs) => from rhs in this.verbs where lhs != rhs && lhs.label == rhs.label select rhs).FirstOrDefault();
                if (dupeVerb != null)
                {
                    yield return string.Format("duplicate hediff verb label {0}", dupeVerb.label);
                }
                PCF_VerbProperties dupeVerbProperties = this.verbsProperties.SelectMany((PCF_VerbProperties lhs) => from rhs in this.verbsProperties where lhs != rhs && lhs.label == rhs.label select rhs).FirstOrDefault();
                if (dupeVerbProperties != null)
                {
                    yield return string.Format("duplicate hediff verb properties label {0}", dupeVerbProperties.label);
                }
            }
        }

        public string toggleLabel;

        [Description("A human-readable description given when the Def is inspected by players.")]
        [DefaultValue(null)]
        [MustTranslate]
        public string toggleDescription;

        [NoTranslate]
        public string toggleIconPath;

        public float toggleIconAngle;

        public Vector2 toggleIconOffset;

        public List<VerbProperties> verbs;

        public List<PCF_VerbProperties> verbsProperties;
    }

    public class PCF_VerbProperties
    {
        public string label;

        [Description("A human-readable description given when the Verb is inspected by players.")]
        [DefaultValue(null)]
        [MustTranslate]
        public string description;

        [NoTranslate]
        public string uiIconPath;

        public float uiIconAngle;

        public Vector2 uiIconOffset;
    }
}
