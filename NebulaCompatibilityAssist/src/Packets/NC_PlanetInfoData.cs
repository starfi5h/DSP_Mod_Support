using NebulaAPI.Networking;
using NebulaAPI.Packets;
using NebulaCompatibilityAssist.Patches;
using System.Collections.Generic;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_PlanetInfoData
    {
        public int StarId { get; set; }
        public int PlanetId { get; set; }
        public long EnergyCapacity { get; set; }
        public long EnergyRequired { get; set; }
        public long EnergyExchanged { get; set; }
        public int NetworkCount { get; set; }
        public float[] ConsumerRatios { get; set; }
        public int[] ConsumerCounts { get; set; }

        public NC_PlanetInfoData() { }
        public NC_PlanetInfoData(in PlanetData planet)
        {
            PlanetId = planet.id;
            var ratios = new List<float>();
            var counts = new List<int>();
            if (planet.factory?.powerSystem != null)
            {
                for (int i = 1; i < planet.factory.powerSystem.netCursor; i++)
                {
                    PowerNetwork powerNetwork = planet.factory.powerSystem.netPool[i];
                    if (powerNetwork != null && powerNetwork.id == i)
                    {
                        NetworkCount++;
                        EnergyCapacity += powerNetwork.energyCapacity;
                        EnergyRequired += powerNetwork.energyRequired;
                        EnergyExchanged += powerNetwork.energyExchanged;
                        ratios.Add((float)powerNetwork.consumerRatio);
                        counts.Add(powerNetwork.consumers.Count);
                    }
                }
            }
            ConsumerRatios = ratios.ToArray();
            ConsumerCounts = counts.ToArray();
        }

        public NC_PlanetInfoData(in StarData star)
        { 
            StarId = star.id;

            var ratios = new List<float>();
            var counts = new List<int>();
            for (int j = 0; j < star.planetCount; j++)
            {
                PlanetData planet = star.planets[j];
                if (planet.factory?.powerSystem != null)
                {
                    for (int i = 1; i < planet.factory.powerSystem.netCursor; i++)
                    {
                        PowerNetwork powerNetwork = planet.factory.powerSystem.netPool[i];
                        if (powerNetwork != null && powerNetwork.id == i)
                        {
                            NetworkCount++;
                            EnergyCapacity += powerNetwork.energyCapacity;
                            EnergyRequired += powerNetwork.energyRequired;
                            EnergyExchanged += powerNetwork.energyExchanged;
                            ratios.Add((float)powerNetwork.consumerRatio);
                            counts.Add(powerNetwork.consumers.Count);
                        }
                    }
                }
            }
            ConsumerRatios = ratios.ToArray();
            ConsumerCounts = counts.ToArray();
        }
    }

    [RegisterPacketProcessor]
    internal class NC_PlanetInfoDataProcessor : BasePacketProcessor<NC_PlanetInfoData>
    {
        public override void ProcessPacket(NC_PlanetInfoData packet, INebulaConnection conn)
        {
            if (IsHost) return;

            if (PlanetFinder_Patch.Enable)
                PlanetFinder_Patch.OnReceive(packet);
            if (FactoryLocator_Patch.Enable)
                FactoryLocator_Patch.OnReceive(packet);
        }
    }
}
