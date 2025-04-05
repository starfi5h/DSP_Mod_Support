using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: AssemblyTitle(RateMonitor.Plugin.NAME)]
[assembly: AssemblyVersion(RateMonitor.Plugin.VERSION)]

namespace RateMonitor
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.RateMonitor";
        public const string NAME = "RateMonitor";
        public const string VERSION = "0.1.0";

        public static ManualLogSource Log;
        public static Plugin instance;
        static Harmony harmony;

        public static StatTable MainTable { get; set; }
        public static EOperation Operation { get; set; }
        public static string LastStatInfo { get; private set; } = "";
        static List<int> lastEntityIds;
        static int lastPlanetId;

        public enum EOperation
        {
            Normal,
            Add,
            Sub
        }

        public void Awake()
        {
            Log = Logger;
            instance = this;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(SelectionTool_Patches));
            harmony.PatchAll(typeof(Plugin));
            ModSettings.LoadConfigs(Config);

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("org.LoShin.GenesisBook", out var _))
            {
                Log.LogInfo("Compat - GenesisBook");
                CalDB.CompatGB = true;
            }

            UI.Utils.Init(16.0f);
        }

        public void OnGUI()
        {
            UI.UIWindow.OnGUI();
        }

        public static void OnSelectionFinish(PlanetFactory factory, HashSet<int> entityIdSet)
        {
            switch (Operation)
            {
                case EOperation.Add:
                {
                    Operation = EOperation.Normal;
                    if (MainTable == null) return;
                    var currentList = MainTable.GetEntityIds(out var currentFactory);
                    if (currentFactory != factory) return;
                    entityIdSet.UnionWith(currentList);
                    if (entityIdSet.Count() > 0)
                    {
                        SaveCurrentTable();
                        CreateMainTable(factory, entityIdSet.ToList());
                    }
                    break;
                }
                case EOperation.Sub:
                {
                    Operation = EOperation.Normal;
                    if (MainTable == null) return;
                    var currentSet = MainTable.GetEntityIds(out var currentFactory);
                    if (currentFactory != factory) return;
                    var interset = entityIdSet.Intersect(currentSet);
                    foreach (var removeId in interset)
                    {
                        currentSet.Remove(removeId);
                    }
                    if (interset.Count() > 0)
                    {
                        SaveCurrentTable();
                        CreateMainTable(factory, currentSet.ToList());
                    }
                    break;
                }

                default:
                    SaveCurrentTable();
                    CreateMainTable(factory, entityIdSet.ToList());
                    break;
            }            
        }

        public static void CreateMainTable(PlanetFactory factory, List<int> entityIds)
        {
            MainTable = new StatTable();
            MainTable.Initialize(factory, entityIds);
            MainTable.PrintRefRates();
            MainTable.PrintProifles();
            Log.LogDebug("CreateMainTable: " + MainTable.GetEntityCount());
        }

        public static bool SaveCurrentTable()
        {
            if (MainTable == null) return false;
            var list = MainTable.GetEntityIds(out var factory);
            if (list.Count == 0) return false;
            lastEntityIds = list;
            lastPlanetId = factory.planetId;
            var planet = GameMain.galaxy.PlanetById(lastPlanetId);
            LastStatInfo = (planet?.displayName ?? " ") + ": " + lastEntityIds.Count;
            Log.LogDebug("SaveCurrentTable: " + LastStatInfo);
            return true;
        }

        public static bool LoadLastTable()
        {
            var factory = GameMain.galaxy.PlanetById(lastPlanetId)?.factory;
            if (factory == null || lastEntityIds.Count == 0) return false;

            var entityIds = new List<int>();
            foreach (int entityId in lastEntityIds)
            {
                if (SelectionTool.ShouldAddObject(factory, entityId)) entityIds.Add(entityId);
            }
            if (entityIds.Count == 0) return false;

            SaveCurrentTable();
            CreateMainTable(factory, entityIds);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static void UpdateMonitor(bool __runOriginal)
        {
            // If other mods have stop factory simulation, then skip
            if (MainTable == null || !__runOriginal) return;

            try
            {
                MainTable.OnGameTick();
            }
            catch (Exception ex)
            {
                Log.LogError(ex);
                MainTable.OnError();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        static void OnGameEnd()
        {
            MainTable = null;
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
