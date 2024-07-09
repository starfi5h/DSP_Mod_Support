using HarmonyLib;
using System.Collections.Generic;

namespace MassRecipePaste
{
    public class ExtraCopy
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.OnApplyClick))]
        internal static void OnApplyClick()
        {
            Plugin.instance.Config.Reload(); // Reload config file when clicking 'Apply' in game settings
            Plugin.instance.LoadConfigs();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CopyBuildingSetting))]
        public static void CopyExtraClipborad(PlanetFactory __instance, int objectId)
        {
            if (objectId <= 0) return;
            if (__instance.entityPool[objectId].id != objectId) return;
            var stationId = __instance.entityPool[objectId].stationId;

            if (stationId != 0)
            {
                var stationComponent = __instance.transport.stationPool[stationId];
                StationParameters.Copy(__instance, stationComponent);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.PasteBuildingSetting))]
        public static void PasteExtraClipborad(PlanetFactory __instance, int objectId)
        {
            if (objectId <= 0 || !Patches.tool.isPasting) return;
            if (__instance.entityPool[objectId].id != objectId) return;
            var stationId = __instance.entityPool[objectId].stationId;

            if (stationId != 0)
            {
                var stationComponent = __instance.transport.stationPool[stationId];
                StationParameters.Paste(__instance, stationComponent);
            }
        }
    }

    static class StationParameters
    {
        static string name;
        static long remoteGroupMask;
        static ERemoteRoutePriority routePriority;
        static readonly List<int> addGids = new();

        public static void Copy(PlanetFactory factory, StationComponent station)
        {
            // Limit to ILS currently
            if (!station.isStellar) return;

            remoteGroupMask = station.remoteGroupMask;
            routePriority = station.routePriority;
            if (Plugin.CopyStationName.Value)
            {
                name = factory.ReadExtraInfoOnEntity(station.entityId);
            }
            if (Plugin.CopyStationP2P.Value)
            {
                addGids.Clear();
                var galacticTransport = GameMain.data.galacticTransport;
                for (int i = 1; i < galacticTransport.stationCursor; i++)
                {
                    if (galacticTransport.stationPool[i] != null && galacticTransport.stationPool[i].id > 0 && galacticTransport.stationPool[i].gid == i)
                    {
                        long key = galacticTransport.CalculateStation2StationKey(station.gid, i);
                        if (galacticTransport.station2stationRoutes.Contains(key))
                        {
                            addGids.Add(i);
                        }
                    }
                }
            }
        }

        public static void Paste(PlanetFactory factory, StationComponent station)
        {
            // Limit to ILS currently
            if (!station.isStellar) return;

            if (Plugin.CopyStationGroup.Value)
            {
                station.remoteGroupMask = remoteGroupMask;
            }
            if (Plugin.CopyStationPriorityBehavior.Value)
            {
                station.routePriority = routePriority;
            }
            if (Plugin.CopyStationName.Value)
            {
                factory.WriteExtraInfoOnEntity(station.entityId, name);
            }
            if (Plugin.CopyStationP2P.Value)
            {
                GameMain.data.galacticTransport.RemoveStation2StationRoute(station.gid);
                foreach (var i in addGids)
                {
                    GameMain.data.galacticTransport.AddStation2StationRoute(station.gid, i);
                }
            }
        }
    }
}
