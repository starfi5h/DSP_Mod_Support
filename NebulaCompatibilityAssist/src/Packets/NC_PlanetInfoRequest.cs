using NebulaAPI;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_PlanetInfoRequest
    {
        public int AstroId { get; set; }

        public NC_PlanetInfoRequest() { }
        public NC_PlanetInfoRequest(int astroId)
        {
            AstroId = astroId;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_PlanetInfoRequestProcessor : BasePacketProcessor<NC_PlanetInfoRequest>
    {
        public override void ProcessPacket(NC_PlanetInfoRequest packet, INebulaConnection conn)
        {
            if (IsClient) return;

            if (packet.AstroId == -1) // all
            {
                for (int i = 0; i < GameMain.data.factoryCount; i++)
                {
                    // Send infomation of planet that has factory on it
                    conn.SendPacket(new NC_PlanetInfoData(GameMain.data.factories[i].planet));
                }
            }
            else if (packet.AstroId > 0)
            {
                if (packet.AstroId % 100 == 0) // star
                {
                    StarData star = GameMain.data.galaxy.StarById(packet.AstroId/100);
                    Log.Debug(packet.AstroId / 100);
                    Log.Debug(star);
                    if (star != null)
                        conn.SendPacket(new NC_PlanetInfoData(star));
                }
                else
                {
                    PlanetData planet = GameMain.data.galaxy.PlanetById(packet.AstroId);
                    if (planet != null)
                        conn.SendPacket(new NC_PlanetInfoData(planet));
                }
            }
        }
    }
}
