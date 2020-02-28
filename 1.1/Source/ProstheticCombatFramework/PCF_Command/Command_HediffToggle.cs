using System;
using UnityEngine;
using RimWorld;
using Verse;

namespace OrenoPCF
{
    public class Command_HediffToggle : Command
    {
        public override SoundDef CurActivateSound
        {
            get
            {
                if (this.isActive())
                {
                    return this.turnOffSound;
                }
                return this.turnOnSound;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            this.toggleAction();
        }

        public override GizmoResult GizmoOnGUI(Vector2 loc, float maxWidth)
        {
            GizmoResult result = base.GizmoOnGUI(loc, maxWidth);
            Rect rect = new Rect(loc.x, loc.y, this.GetWidth(maxWidth), 75f);
            Rect position = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
            Texture2D image = (!this.isActive()) ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex;
            GUI.DrawTexture(position, image);
            return result;
        }

        public override bool GroupsWith(Gizmo other)
        {
            return false;
        }

        public Func<bool> isActive;

        public Action toggleAction;

        public SoundDef turnOnSound = SoundDefOf.Checkbox_TurnedOn;

        public SoundDef turnOffSound = SoundDefOf.Checkbox_TurnedOff;
    }
}
