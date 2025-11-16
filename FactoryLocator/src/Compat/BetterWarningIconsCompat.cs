using CommonAPI.Systems;
using HarmonyLib;
using System;
using UnityEngine;
using xiaoye97;

namespace FactoryLocator.Compat
{
    public static class BetterWarningIconsCompat
    {
        private const string GUID = "dev.raptor.dsp.BetterWarningIcons";
        // last target version: 0.0.5

        private const int InsufficientInputSignalId = 531;

        public static void Preload()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out _))
                return;

            SignalProto proto = new()
            {
                ID = InsufficientInputSignalId,
                IconPath = "",
                Name = "Insufficient Input",
                GridIndex = 3601,
                description = "Building does not have sufficient inputs to work"
            };
            LDBTool.PreAddProto(proto);
        }


        public static void Postload()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out _))
                return;

            try
            {
                SetIcon();
            }
            catch (Exception e)
            {
                Log.Warn($"BetterWarningIcons compat fail! Last target version: 0.0.5");
                Log.Debug(e);
            }
        }

        private static void SetIcon()
        {
            var signal = LDB.signals.Select(InsufficientInputSignalId);
            signal.Name = "Insufficient Input";
            signal.name = "Insufficient Input".Translate();
            // The icon spirte setter gets NRE error
            //signal._iconSprite = (Sprite)AccessTools.TypeByName("DysonSphereProgram.Modding.BetterWarningIcons.InsufficientInputIconPatch").GetField("iconSprite").GetValue(null);
            //Log.Debug(signal.name);
        }
    }
}
