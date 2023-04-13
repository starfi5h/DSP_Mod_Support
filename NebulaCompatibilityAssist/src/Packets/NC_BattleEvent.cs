using NebulaAPI;
using NebulaCompatibilityAssist.Patches;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_BattleEvent
    {
        public enum EType
        {
            Configs,
            RemoveEntities,
            StarCannonStartAiming,
            AddRelic,
            RemoveRelic,
            EnemyShipState,
            StarFortressSetModuleNum
        }

        public EType EventType { get; set; }
        public ushort PlayerId { get; set; }
        public byte[] Bytes { get; set; }

        public NC_BattleEvent() {}
        public NC_BattleEvent(EType eventType, ushort playerId, byte[] bytes)
        {
            EventType = eventType;
            PlayerId = playerId;
            Bytes = bytes;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_BattleEventProcessor : BasePacketProcessor<NC_BattleEvent>
    {
        public override void ProcessPacket(NC_BattleEvent packet, INebulaConnection conn)
        {
            if (IsHost)
            {
                // Broadcast changes to other users
                // NebulaModAPI.MultiplayerSession.Network.SendPacketExclude(packet, conn); //this method is tempoarily broken before fix in 0.8.12
                var playerManager = NebulaModAPI.MultiplayerSession.Network.PlayerManager;
                INebulaPlayer player = playerManager.GetPlayer(conn);
                playerManager.SendPacketToOtherPlayers(packet, player);
            }

            using var p = NebulaModAPI.GetBinaryReader(packet.Bytes);
            var r = p.BinaryReader;
            switch (packet.EventType)
            {
                case NC_BattleEvent.EType.Configs:
                    DSP_Battle_Patch.Warper.SyncConfig(r, packet.PlayerId);
                    break;

                case NC_BattleEvent.EType.RemoveEntities:
                    DSP_Battle_Patch.Warper.SyncRemoveEntities(r);
                    break;

                case NC_BattleEvent.EType.StarCannonStartAiming:
                    DSP_Battle_Patch.Warper.SyncStartAiming(packet.PlayerId);
                    break;

                case NC_BattleEvent.EType.AddRelic:
                    DSP_Battle_Patch.Warper.SyncAddRelic(r);
                    break;

                case NC_BattleEvent.EType.RemoveRelic:
                    DSP_Battle_Patch.Warper.SyncRemoveRelic(r);
                    break;

                case NC_BattleEvent.EType.EnemyShipState:
                    DSP_Battle_Patch.Warper.SyncEnemyShipState(r);
                    break;

                case NC_BattleEvent.EType.StarFortressSetModuleNum:
                    DSP_Battle_Patch.Warper.SyncStarFortressSetModuleNum(r);
                    break;
            }
        }
    }
}
