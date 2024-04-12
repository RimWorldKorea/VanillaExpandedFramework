﻿using System.Linq;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace VanillaGenesExpanded
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "ScaleFor")]
    public static class PawnRenderNodeWorker_ScaleFor_Patch
    {
        public static void Postfix(ref Vector3 __result, PawnRenderNode node, PawnDrawParms parms)
        {
            var pawn = parms.pawn;
            if (node is PawnRenderNode_Body)
            {
                __result = GeneUtils.SetBodyScale(pawn, __result);
            }
            else if (node is PawnRenderNode_Head)
            {
                __result = GeneUtils.SetHeadScale(pawn, __result);
            }

            if (node.gene != null)
            {
                // Commented out because scale on genes is already inherited from the body/head in 1.5 RimWorld.
                //__result = GeneUtils.SetGeneScale(pawn, __result, node.gene);
            }
        }
    }
}
