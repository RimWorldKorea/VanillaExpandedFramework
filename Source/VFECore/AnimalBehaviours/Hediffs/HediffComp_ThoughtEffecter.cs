﻿

using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using Verse.Sound;
using UnityEngine;

namespace AnimalBehaviours
{
    class HediffComp_ThoughtEffecter : HediffComp
    {

        public int tickCounter = 0;
        public List<Pawn> pawnList = new List<Pawn>();
        public Pawn thisPawn;

        public HediffCompProperties_ThoughtEffecter Props
        {
            get
            {
                return (HediffCompProperties_ThoughtEffecter)this.props;
            }
        }


        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
           
                tickCounter++;
                //Only do anything every tickInterval
                if (tickCounter > Props.tickInterval)
                {
                    thisPawn = this.parent.pawn as Pawn;
                    //Null map check. Also will only work if pawn is not dead or downed, and if needsToBeTamed is true, that the animal is tamed
                    if (thisPawn != null && thisPawn.Map != null && !thisPawn.Dead && !thisPawn.Downed && (!Props.needsToBeTamed || (Props.needsToBeTamed && thisPawn.Faction != null && thisPawn.Faction.IsPlayer)))
                    {
                        foreach (Thing thing in GenRadial.RadialDistinctThingsAround(thisPawn.Position, thisPawn.Map, Props.radius, true))
                        {
                            Pawn pawn = thing as Pawn;
                            //It won't affect animals, cause they don't have Thoughts, or mechanoids, or itself
                            if (pawn != null && !pawn.AnimalOrWildMan() && pawn.RaceProps.IsFlesh && pawn != this.parent.pawn)
                            {
                                //Only work on not dead, not downed, not psychically immune pawns
                                if (!pawn.Dead && !pawn.Downed && pawn.GetStatValue(StatDefOf.PsychicSensitivity, true) > 0f)
                                {
                                    //Only show an effect if the user wants it to, or it gets obnoxious
                                    if (Props.showEffect)
                                    {
                                        Find.TickManager.slower.SignalForceNormalSpeedShort();
                                        SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(this.parent.pawn.Position, this.parent.pawn.Map, false));
                                        FleckMaker.AttachedOverlay(this.parent.pawn, DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect"), Vector3.zero, 1f, -1f);
                                    }
                                    if (!Props.conditionalOnWellBeing)
                                    {
                                        //Apply thought
                                        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named(Props.thoughtDef), null);
                                    }
                                    else
                                    {
                                        bool wellbeingAffectedFlag = thisPawn.needs.food.Starving || (thisPawn.health.hediffSet.PainTotal > 0);
                                        if (wellbeingAffectedFlag)
                                        {
                                            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDef.Named(Props.thoughtDef));
                                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named(Props.thoughtDefWhenSuffering), null);
                                        }
                                        else
                                        {

                                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named(Props.thoughtDef), null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    tickCounter = 0;
                
            }

        }



    }
}
