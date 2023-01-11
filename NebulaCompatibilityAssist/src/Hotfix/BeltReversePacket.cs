using HarmonyLib;
using NebulaAPI;
using System;

namespace NebulaCompatibilityAssist.Hotfix
{
    public partial class Build15466_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBeltWindow), "OnReverseButtonClick")]
        public static void OnReverseButtonClick_Postfix(UIBeltWindow __instance)
        {
            // Notify others about belt direction reverse
            if (NebulaModAPI.IsMultiplayerActive && !NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.Value)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(new BeltReversePacket(__instance.beltId, __instance.factory.planetId));
            }
        }
    }

    public class BeltReversePacket
    {
        public int BeltId { get; set; }
        public int PlanetId { get; set; }

        public BeltReversePacket() { }
        public BeltReversePacket(int beltId, int planetId)
        {
            BeltId = beltId;
            PlanetId = planetId;
        }
    }

    [RegisterPacketProcessor]
    internal class BeltReverseProcessor : BasePacketProcessor<BeltReversePacket>
    {
        public override void ProcessPacket(BeltReversePacket packet, INebulaConnection conn)
        {            
            if (IsHost) // Broadcast changes to other users
            {
                int starId = GameMain.galaxy.PlanetById(packet.PlanetId).star.id;
                NebulaModAPI.MultiplayerSession.Network.SendPacketToStarExclude(packet, starId, conn); 
            }

            PlanetFactory factory = GameMain.galaxy.PlanetById(packet.PlanetId).factory;
            if (factory == null)
                return;

            using (NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.On())
            {
                NebulaModAPI.MultiplayerSession.Factories.EventFactory = factory;
                NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = packet.PlanetId;
                if (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    // Load planet model
                    NebulaModAPI.MultiplayerSession.Factories.AddPlanetTimer(packet.PlanetId);
                }

                UIBeltWindow beltWindow = UIRoot.instance.uiGame.beltWindow;
                beltWindow._Close(); // close the window first to avoid changing unwant variable when setting beltId
                var tmpFactory = beltWindow.factory;
                var tmpBeltId = beltWindow.beltId;
                beltWindow.factory = factory;
                beltWindow.beltId = packet.BeltId;
                AccessTools.Method(typeof(UIBeltWindow), "OnReverseButtonClick").Invoke(beltWindow, new object[] { 0 });
                beltWindow.factory = tmpFactory;
                beltWindow.beltId = tmpBeltId;

                NebulaModAPI.MultiplayerSession.Factories.EventFactory = null;
                NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = NebulaModAPI.PLANET_NONE;
            }
        }
    }
}
