using NebulaAPI.Networking;
using NebulaAPI.Packets;
using NebulaCompatibilityAssist.Patches;
using NebulaWorld;
using System.Collections.Generic;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_BattleUpdate
    {
        public string Username { get; set; }
        public EType Type { get; set; }
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int[] Values1 { get; set; }
        public int[] Values2 { get; set; }

        public NC_BattleUpdate() { }
        public NC_BattleUpdate(EType type, int value1, int value2)
        {
            Username = Multiplayer.Session.LocalPlayer.Data.Username;
            Type = type;
            Value1 = value1;
            Value2 = value2;
            Values1 = Values2 = new int[0];
        }
        public NC_BattleUpdate(EType type, int[] values1, int[] values2)
        {
            Username = Multiplayer.Session.LocalPlayer.Data.Username;
            Type = type;
            Values1 = values1;
            Values2 = values2;
        }


        public enum EType
        {
            None,
            AddRelic,
            RemoveRelic,
            ApplyAuthorizationPoint,
            ResetAuthorizationPoint
    }
    }

    [RegisterPacketProcessor]
    internal class NC_BattleUpdateProcessor : BasePacketProcessor<NC_BattleUpdate>
    {
        public override void ProcessPacket(NC_BattleUpdate packet, INebulaConnection conn)
        {
            DSP_Battle_Patch.OnReceive(packet);
        }
    }
}
