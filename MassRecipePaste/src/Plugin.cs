﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyTitle(MassRecipePaste.Plugin.NAME)]
[assembly: AssemblyVersion(MassRecipePaste.Plugin.VERSION)]

namespace MassRecipePaste
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.MassRecipePaste";
        public const string NAME = "MassRecipePaste";
        public const string VERSION = "1.1.3";

        public static ManualLogSource Log;
        public static Plugin instance;
        static Harmony harmony;

        public static ConfigEntry<KeyboardShortcut> MassPasteKey;
        public static ConfigEntry<bool> CopyStationName;
        public static ConfigEntry<bool> CopyStationPriorityBehavior;
        public static ConfigEntry<bool> CopyStationGroup;
        public static ConfigEntry<bool> CopyStationP2P;

        public void Awake()
        {
            Log = Logger;
            instance = this;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Patches));
            harmony.PatchAll(typeof(ExtraCopy));
            LoadConfigs();
            Log.LogDebug(VERSION);
        }

        public void LoadConfigs()
        {
            MassPasteKey = Config.Bind("KeyBinds", "MassPasteKey", new KeyboardShortcut(KeyCode.Period, KeyCode.LeftControl), "Custom keybind. Default is ctrl + >(paste recipe)\n没有设置时, 默认为Ctrl + >(配方黏贴键)");
            if (!MassPasteKey.Value.Equals(default(KeyboardShortcut)) && !MassPasteKey.Value.Equals(new KeyboardShortcut(KeyCode.Period, KeyCode.LeftControl)))
            {
                Patches.isCustomHotkey = true;
                Logger.LogDebug("MassPasteKey: " + MassPasteKey.Value);
            }
            CopyStationName = Config.Bind("ExtraCopy", "CopyStationName", false, "复制物流站名称");
            CopyStationPriorityBehavior = Config.Bind("ExtraCopy", "CopyStationPriorityBehavior", true, "复制物流站优先行为");
            CopyStationGroup = Config.Bind("ExtraCopy", "CopyStationGroup", true, "复制物流站分组设置");
            CopyStationP2P = Config.Bind("ExtraCopy", "CopyStationP2P", true, "复制物流站点对点设置");
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
