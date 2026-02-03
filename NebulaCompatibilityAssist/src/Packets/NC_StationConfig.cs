using NebulaAPI.Networking;
using NebulaAPI.Packets;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_StationConfig
    {
        public int PlanetId { get; set; }
        public int[] StationIds { get; set; }
        public long[] MaxChargePower { get; set; }
        public double[] MaxTripDrones { get; set; }
        public double[] MaxTripVessel { get; set; }
        public int[] MinDeliverDrone { get; set; }
        public int[] MinDeliverVessel { get; set; }
        public double[] WarpDistance { get; set; }
        public bool[] WarperNeeded { get; set; }
        public bool[] IncludeCollectors { get; set; }
        public int[] PilerCount { get; set; }
        public int[] MaxMiningSpeed { get; set; }

        public NC_StationConfig() { }
        public NC_StationConfig(in StationComponent station, in PlanetFactory factory) :
            this(new StationComponent[1] { station }, factory) { }
        public NC_StationConfig(in StationComponent[] stations, in PlanetFactory factory)
        {
            int length = stations.Length < factory.transport.stationCursor ? stations.Length : factory.transport.stationCursor;
            PlanetId = factory.planetId;
            StationIds = new int[length];
            MaxChargePower = new long[length];
            MaxTripDrones = new double[length];
            MaxTripVessel = new double[length];
            MinDeliverDrone = new int[length];
            MinDeliverVessel = new int[length];
            WarpDistance = new double[length];
            WarperNeeded = new bool[length];
            IncludeCollectors = new bool[length];
            PilerCount = new int[length];
            MaxMiningSpeed = new int[length];

            for (int i = 0; i < length; i++)
            {
                // https://github.com/soarqin/DSP_Mods/blob/master/UXAssist/Patches/LogisticsPatch.cs
                // DoConfigStation
                if (stations[i] != null)
                {
                    StationComponent station = stations[i];
                    
                    StationIds[i] = station.id;
                    if (station.isCollector) continue;
                    
                    MaxChargePower[i] = factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick;
                    MaxTripDrones[i] = station.tripRangeDrones;
                    MaxTripVessel[i] = station.tripRangeShips;
                    MinDeliverDrone[i] = station.deliveryDrones;
                    MinDeliverVessel[i] = station.deliveryShips;
                    WarpDistance[i] = station.warpEnableDist;
                    WarperNeeded[i] = station.warperNecessary;
                    IncludeCollectors[i] = station.includeOrbitCollector;
                    PilerCount[i] = station.pilerCount;
                    if (station.isVeinCollector)
                    {
                        /* station.minerId is not set at this point, so we need to fetch the minerId from the EntityData */
                        ref var entity = ref factory.entityPool[station.entityId];
                        MaxMiningSpeed[i] = factory.factorySystem.minerPool[entity.minerId].speed;
                        Log.Debug($"Advance Miner [{i}]: minerId:{entity.minerId} mining speed {(MaxMiningSpeed[i] / 100f).ToString("0")}%");
                    }
                }
            }
        }
    }

    [RegisterPacketProcessor]
    internal class NC_StationConfigProcessor : BasePacketProcessor<NC_StationConfig>
    {
        public override void ProcessPacket(NC_StationConfig packet, INebulaConnection conn)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null) return;

            Log.Debug($"Update stations config on {packet.PlanetId}: {packet.StationIds.Length}");

            StationComponent[] pool = factory.transport.stationPool;
            for (int i = 0; i < packet.StationIds.Length; i++)
            {
                if (packet.StationIds[i] == 0) continue;
                StationComponent station = pool[packet.StationIds[i]];
                if (station == null)
                {
                    Log.Warn($"Station {i} doesn't exist!");
                    continue;
                }
                if (station.isCollector) continue;

                factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = packet.MaxChargePower[i];
                station.tripRangeDrones = packet.MaxTripDrones[i];
                station.tripRangeShips = packet.MaxTripVessel[i];
                station.deliveryDrones = packet.MinDeliverDrone[i];
                station.deliveryShips = packet.MinDeliverVessel[i];
                station.warpEnableDist = packet.WarpDistance[i];
                station.warperNecessary = packet.WarperNeeded[i];
                station.includeOrbitCollector = packet.IncludeCollectors[i];
                station.pilerCount = packet.PilerCount[i];
                if (station.isVeinCollector)
                {                    
                    ref var entity = ref factory.entityPool[station.entityId];
                    factory.factorySystem.minerPool[entity.minerId].speed = packet.MaxMiningSpeed[i];
                    Log.Debug($"Advance Miner [{i}]: minerId:{entity.minerId} mining speed {(packet.MaxMiningSpeed[i] / 100f).ToString("0")}%");
                }
            }

            // Refresh station window if it is veiwing the changing factory
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
            if (stationWindow != null && stationWindow.active)
            {
                if (stationWindow.factory == factory)
                {
                    stationWindow.OnStationIdChange();
                }
            }
        }
    }

}
