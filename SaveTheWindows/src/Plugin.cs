using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(SaveTheWindows.Plugin.NAME)]
[assembly: AssemblyVersion(SaveTheWindows.Plugin.VERSION)]

namespace SaveTheWindows
{
    public enum ESortOrder
    {
        NameAsc,
        NameDesc,
        DateAsc,
        DateDesc,
        SizeAsc,
        SizeDesc
    }

    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.SaveTheWindows";
        public const string NAME = "SaveTheWindows";
        public const string VERSION = "1.1.1";

        public static ManualLogSource Log;
        public static ConfigFile ConfigFile;
        public static ConfigEntry<string> SubFolder;
        public static ConfigEntry<ESortOrder> SaveOrder;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            ConfigFile = Config;

            var saveWindowPosition = Config.Bind("Config", "Enable Save Window Position", true, "启用窗口位置保存");
            var dragWindowOffset = Config.Bind("Config", "Enable Drag Window Offset", true, "允许窗口部分超出边框");
            var enableSaveSubfolder = Config.Bind("Config", "Enable Save Subfolder", true, "允许存档子文件夹功能");
            SubFolder = Config.Bind("Config", "Save Subfolder", "", "Name of the current subfolder\n当前存档子文件夹名称(空字串=原位置)");
            SaveOrder = Config.Bind("Config", "Save Order", ESortOrder.NameAsc, "Sort order of save files.\n存档排序的方式");
            SaveOrder.SettingChanged += (_, _) => SaveFolder_Patch.OnSaveOrderChange();

            harmony = new Harmony(GUID);
            if (saveWindowPosition.Value)
                harmony.PatchAll(typeof(SaveWindow_Patch));
            if (dragWindowOffset.Value)
                harmony.PatchAll(typeof(UIWindowDragOffset_Patch));
            if (enableSaveSubfolder.Value)
            {
                Logger.LogInfo("Save subfolder enable. Name:" + SubFolder.Value);
                harmony.PatchAll(typeof(SaveFolder_Patch));
            }

#if DEBUG
            SaveWindow_Patch.OpenGameUI();
            SaveFolder_Patch.Init();
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
            SaveFolder_Patch.OnDestroy();
        }
#else
        }
#endif
    }
}
