using NebulaAPI.Networking;
using NebulaAPI.Packets;
using NebulaCompatibilityAssist.Patches;
using System.Collections.Generic;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_LocatorFilter
    {
        public int AstroId { get; set; }
        public int QueryType { get; set; }
        public int Mode { get; set; }
        public int[] Ids { get; set; }
        public int[] Counts { get; set; }

        public NC_LocatorFilter() { }
        public NC_LocatorFilter(int astroId, int queryType, int mode, Dictionary<int, int> filter)
        {
            AstroId = astroId;
            QueryType = queryType;
            Mode = mode;
            if (filter != null)
            {
                Ids = new int[filter.Count];
                Counts = new int[filter.Count];
                int i = 0;
                foreach (var pair in filter)
                {
                    Ids[i] = pair.Key;
                    Counts[i++] = pair.Value;
                }
            }
        }
    }

    [RegisterPacketProcessor]
    internal class NC_LocatorFilterProcessor : BasePacketProcessor<NC_LocatorFilter>
    {
        public override void ProcessPacket(NC_LocatorFilter packet, INebulaConnection conn)
        {
            FactoryLocator_Patch.OnReceive(packet, conn);
        }
    }
}
