﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PipeSystem
{
    public class CompDeepExtractor : CompResource
    {
        public List<IntVec3> lumpCells;

        private List<IntVec3> adjCells;
        private CompPowerTrader compPower;
        private CompFlickable compFlickable;
        private int nextProduceTick = -1;
        private bool noCapacity = false;

        private int count = 0;

        public new CompProperties_DeepExtractor Props => (CompProperties_DeepExtractor)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            compPower = parent.GetComp<CompPowerTrader>();
            compFlickable = parent.GetComp<CompFlickable>();

            adjCells = GenAdj.CellsAdjacent8Way(parent).ToList();
            // Get the cells
            lumpCells = new List<IntVec3>();
            var treated = new HashSet<IntVec3>();
            var toCheck = new Queue<IntVec3>();

            var thing = Props.deepThing.defName;
            var cell = parent.Position;
            var map = parent.Map;

            toCheck.Enqueue(cell);
            treated.Add(cell);

            while (toCheck.Count > 0)
            {
                var temp = toCheck.Dequeue();
                lumpCells.Add(temp);

                var neighbours = GenAdjFast.AdjacentCellsCardinal(temp);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    var n = neighbours[i];
                    if (n.InBounds(map) && !treated.Contains(n) && map.deepResourceGrid.ThingDefAt(n) is ThingDef r && r.defName == thing)
                    {
                        count += map.deepResourceGrid.CountAt(n);
                        treated.Add(n);
                        toCheck.Enqueue(n);
                    }
                }
            }

            lumpCells.SortByDescending(c => c.DistanceTo(cell));

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                StartSustainer();
            });
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            nextProduceTick = -1;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextProduceTick, "nextProduceTick", 0);
            Scribe_Values.Look(ref noCapacity, "noCapacity", false);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.Spawned && lumpCells.Count > 0)
            {
                var ticksGame = Find.TickManager.TicksGame;
                if (nextProduceTick == -1)
                {
                    nextProduceTick = ticksGame + Props.ticksPerPortion;
                }
                else if (ticksGame >= nextProduceTick)
                {
                    TryProducePortion();
                    nextProduceTick = ticksGame + Props.ticksPerPortion;
                }

                sustainer?.Maintain();
            }
            else
            {
                EndSustainer();
            }
        }

        public override string CompInspectStringExtra()
        {
            var str = base.CompInspectStringExtra();

            if (parent.Spawned)
            {
                str += "\n" + "PipeSystem_ResourceLeft".Translate(count);
                if (noCapacity)
                {
                    str += "\n" + Props.noStorageLeftKey.Translate();
                }
                if (lumpCells.Count == 0)
                {
                    str += "\n" + "DeepDrillNoResources".Translate();
                }
            }
            return str;
        }

        private void TryProducePortion()
        {
            // No power -> Return
            if ((compPower != null && !compPower.PowerOn) || (compFlickable != null && !compFlickable.SwitchIsOn))
            {
                EndSustainer();
                return;
            }
            // Get resource
            bool nextResource = GetNextResource(out ThingDef resDef, out int count, out IntVec3 cell);
            // Resource is null or not the wanted one -> Return
            if (resDef == null || resDef.defName != Props.deepThing.defName)
                return;

            var map = parent.Map;
            // Resource comp is here
            if (nextResource && !Props.onlyExtractToGround && PipeNet is PipeNet net && net.connectors.Count > 1)
            {
                var available = (int)net.AvailableCapacity;
                noCapacity = available <= 1;

                var extractAmount = Math.Min(Math.Min(resDef.deepCountPerPortion, count), available);
                if (!noCapacity)
                {
                    parent.Map.deepResourceGrid.SetAt(cell, resDef, count - extractAmount);
                    net.DistributeAmongStorage(extractAmount);
                    this.count -= extractAmount;
                    StartSustainer();
                }
                else
                {
                    EndSustainer();
                }
            }
            // If there is no resource comp
            // Deplete resource by one
            else if (nextResource && !Props.onlyExtractToNet)
            {
                StartSustainer();
                var extractAmount = Math.Min(resDef.deepCountPerPortion, count);
                parent.Map.deepResourceGrid.SetAt(cell, resDef, count - resDef.deepCountPerPortion);
                // Spawn items
                var t = ThingMaker.MakeThing(resDef);
                t.stackCount = extractAmount;
                GenPlace.TryPlaceThing(t, adjCells.RandomElement(), map, ThingPlaceMode.Near);
            }

            lumpCells.RemoveAll(c => map.deepResourceGrid.ThingDefAt(c) == null);
        }

        private bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            var map = parent.Map;
            if (lumpCells.Count > 0)
            {
                var c = lumpCells[0];

                if (map.deepResourceGrid.ThingDefAt(c) is ThingDef r)
                {
                    resDef = r;
                    countPresent = map.deepResourceGrid.CountAt(c);
                    cell = c;
                    return true;
                }
                else
                {
                    resDef = null;
                    countPresent = 0;
                    cell = c;
                    lumpCells.RemoveAt(0);
                    return false;
                }
            }
            resDef = null;
            countPresent = 0;
            cell = IntVec3.Invalid;
            return false;
        }
    }
}