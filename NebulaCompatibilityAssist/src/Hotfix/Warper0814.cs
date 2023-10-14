using HarmonyLib;
using NebulaCompatibilityAssist.Packets;
using NebulaModel.Packets.Logistics;
using NebulaWorld;
using NebulaWorld.Logistics;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class Warper0814
    {
        #region station

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationUIManager), nameof(StationUIManager.UpdateStorage))]
        public static bool UpdateStorage_Prefix(StorageUI packet)
        {
            StationComponent stationComponent = StationUIManager.GetStation(packet.PlanetId, packet.StationId, packet.StationGId);
            if (stationComponent == null || stationComponent.storage?.Length == 0)
            {
                return false;
            }
            return true;
        }


        #endregion

        static int queryingIndex = -1;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.OnCursorFunction2Click))]
        public static void QueryDysonSphere(UIStarmap __instance)
        {
            // Client: Query existing dyson sphere when clicking on 'View' star on starmap
            if (!Multiplayer.IsActive || Multiplayer.Session.LocalPlayer.IsHost) return;
            if (Multiplayer.Session.IsInLobby) return;
            if (__instance.focusStar == null) return;

            int starIndex = __instance.focusStar.star.index;
            if (GameMain.data.dysonSpheres[starIndex] == null)
            {
                if (queryingIndex != starIndex)
                {
                    Multiplayer.Session.Network.SendPacket(new NC_QueryDysonSphere(starIndex));
                    queryingIndex = starIndex;
                }
            }
        }
    }
}
