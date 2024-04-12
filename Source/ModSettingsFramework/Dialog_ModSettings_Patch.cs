﻿using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ModSettingsFramework
{
    [HarmonyPatch(typeof(Dialog_ModSettings), "DoWindowContents")]
    public static class Dialog_ModSettings_Patch
    {
        public static void Postfix(Rect inRect, Dialog_ModSettings __instance)
        {
            if (__instance.mod is ModSettingsFrameworkMod modSettings)
            {
                Text.Font = GameFont.Small;
                if (Widgets.ButtonText(new Rect(inRect.width - 180, 0f, 150f, 35), "Reset".Translate()))
                {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    var container = ModSettingsFrameworkSettings
                        .GetModSettingsContainer(modSettings.modPackOverride.PackageIdPlayerFacing);
                    var patches = container.PatchOperationModSettings;
                    foreach (var patch in patches)
                    {
                        var patchContainer = patch.container;
                        patchContainer.patchOperationStates.Remove(patch.patch.id);
                        patchContainer.patchOperationValues.Remove(patch.patch.id);
                        if (patch.patch is PatchOperationWorker patchWorker)
                        {
                            patchWorker.Reset();
                        }
                    }
                }
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                var warningRect = new Rect(inRect.width - 400, 0, 200, 50);
                Widgets.Label(warningRect, "Some changes might require you to restart the game to take effect!");
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }
    }
}
