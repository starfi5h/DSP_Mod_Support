using HarmonyLib;
using NebulaModel.Packets.Logistics;
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
    }
}
