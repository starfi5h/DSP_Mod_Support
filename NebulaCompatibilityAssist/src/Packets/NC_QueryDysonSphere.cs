using NebulaAPI;
using NebulaModel.Networking;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Universe;
using NebulaWorld;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_QueryDysonSphere
    {
        public int StarIndex { get; set; }

        public NC_QueryDysonSphere() {}
        public NC_QueryDysonSphere(int starIndex)
        {
            StarIndex = starIndex;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_QueryDysonSphereProcessor : BasePacketProcessor<NC_QueryDysonSphere>
    {
        public override void ProcessPacket(NC_QueryDysonSphere packet, INebulaConnection conn)
        {
            if (IsHost)
            {
                // Ingroe query if dyson sphere doesn't exist on host
                int starIndex = packet.StarIndex;
                if (starIndex < 0 || (ulong)starIndex >= (ulong)GameMain.data.galaxy.starCount)
                {
                    return;
                }
                DysonSphere dysonSphere = GameMain.data.dysonSpheres[starIndex];
                if (dysonSphere == null)
                {
                    return;
                }
                // Copy from DysonSphereRequestProcessor
                using (BinaryUtils.Writer writer = new BinaryUtils.Writer())
                {
                    dysonSphere.Export(writer.BinaryWriter);
                    byte[] data = writer.CloseAndGetBytes();
                    Log.Info($"Sent {data.Length} bytes of data for DysonSphereData (INDEX: {packet.StarIndex})");
                    conn.SendPacket(new FragmentInfo(data.Length));
                    conn.SendPacket(new DysonSphereData(packet.StarIndex, data, DysonSphereRespondEvent.Load));
                    Multiplayer.Session.DysonSpheres.RegisterPlayer(conn, packet.StarIndex);
                }
            }
        }
    }
}
