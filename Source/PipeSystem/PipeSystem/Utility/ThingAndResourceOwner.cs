﻿using Verse;

namespace PipeSystem
{
    public class ThingAndResourceOwner : IExposable
    {
        private int wantedCount;

        private bool beingFilled;
        private PipeNetDef pipeNetDef;
        private ThingDef thingDef;
        private int count = 0;

        public ThingDef ThingDef => thingDef;

        public PipeNetDef PipeNetDef => pipeNetDef;

        public bool Require => count < wantedCount;

        public bool BeingFilled
        {
            get => beingFilled;
            internal set => beingFilled = value;
        }

        public int Required => wantedCount - count;

        public int Count => count;

        public ThingAndResourceOwner()
        { }

        public ThingAndResourceOwner(ThingDef thingDef, PipeNetDef pipeNetDef, int wantedCount)
        {
            this.pipeNetDef = pipeNetDef;
            this.thingDef = thingDef;
            this.wantedCount = wantedCount;
            beingFilled = false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref wantedCount, "wantedCount");
            Scribe_Values.Look(ref count, "count");
            Scribe_Values.Look(ref beingFilled, "beingFilled");
            Scribe_Defs.Look(ref pipeNetDef, "pipeNetDef");
            Scribe_Defs.Look(ref thingDef, "thingDef");
        }

        public void AddFromThing(Thing thing)
        {
            if (thingDef == null || thing.def != thingDef)
                return;

            var needed = wantedCount - count;
            if (thing.stackCount > needed)
            {
                var taken = thing.SplitOff(needed);
                count += taken.stackCount;
                taken.Destroy();
            }
            else
            {
                count += thing.stackCount;
                thing.Destroy();
            }
        }

        public void AddFromNet(PipeNet net)
        {
            if (pipeNetDef == null || net.def != pipeNetDef)
                return;

            var needed = wantedCount - count;
            var available = (int)net.Stored;

            if (needed > available)
            {
                net.DrawAmongStorage(available, net.storages);
                count += available;
            }
            else
            {
                net.DrawAmongStorage(needed, net.storages);
                count += needed;
            }
        }

        public void Reset()
        {
            count = 0;
            beingFilled = false;
        }

        public override string ToString()
        {
            return $"Owner ({ThingDef?.defName} {PipeNetDef?.defName}): {count}/{wantedCount}";
        }
    }
}