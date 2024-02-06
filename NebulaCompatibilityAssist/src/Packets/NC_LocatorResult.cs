using NebulaAPI.DataStructures;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using NebulaCompatibilityAssist.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_LocatorResult
    {
        public int AstroId { get; set; }
        public int QueryType { get; set; }
        public int ProtoId { get; set; }
        public int[] PlanetIds { get; set; }
        public Float3[] LocalPos { get; set; }
        public int[] DetailIds { get; set; }

        public NC_LocatorResult() { }
        public NC_LocatorResult(int astroId, int queryType, int protoId)
        {
            AstroId = astroId;
            QueryType = queryType;
            ProtoId = protoId;
            PlanetIds = Array.Empty<int>();
            LocalPos = Array.Empty<Float3>();
            DetailIds = Array.Empty<int>();
        }
        public NC_LocatorResult(int queryType, List<int> planetIds, List<Vector3> localPos, List<int> detailIds)
        {
            QueryType = queryType;
            PlanetIds = planetIds.ToArray();
            DetailIds = detailIds.ToArray();
            LocalPos = new Float3[localPos.Count];
            for (int i = 0; i < localPos.Count; i++)
                LocalPos[i] = localPos[i].ToFloat3();
        }
    }

    [RegisterPacketProcessor]
    internal class NC_LocatorResultProcessor : BasePacketProcessor<NC_LocatorResult>
    {
        public override void ProcessPacket(NC_LocatorResult packet, INebulaConnection conn)
        {
            FactoryLocator_Patch.OnReceive(packet, conn);
        }
    }
}
