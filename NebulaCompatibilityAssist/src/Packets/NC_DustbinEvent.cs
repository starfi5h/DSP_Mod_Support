using NebulaAPI;
using NebulaCompatibilityAssist.Patches;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_DustbinEvent
    {
        public int PlanetId { get; set; }
        public int StorageId { get; set; }
        public int TankId { get; set; }
        public bool Enable { get; set; }
        
        public NC_DustbinEvent() { }
        public NC_DustbinEvent(int planetId, int storageId, int tankId, bool enable)
        {
            PlanetId = planetId;
            StorageId = storageId;
            TankId = tankId;
            Enable = enable;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_DustbinEventProcessor : BasePacketProcessor<NC_DustbinEvent>
    {
        public override void ProcessPacket(NC_DustbinEvent packet, INebulaConnection conn)
        {
            Dustbin_Patch.OnEventReceive(packet, conn);
        }
    }
}
