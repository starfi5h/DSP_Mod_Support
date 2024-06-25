using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using NebulaWorld;
using NebulaWorld.Factory;
using UnityEngine;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_UXA_Packet
    {
        public EType Type { get; set; }
        public int PlanetId { get; set; }
        public int AuthorId { get; set; }
        public float Value1 { get; set; }
        public float Value2 { get; set; }
        public float Value3 { get; set; }

        public NC_UXA_Packet() { }
        public NC_UXA_Packet(EType type, int planetId, int authorId)
        {
            Type = type;
            PlanetId = planetId;
            AuthorId = authorId;
        }


        public enum EType
        {
            None,
            InitDysonSphere,
            DismantleAll,
            BuildOrbitalCollector
        }
    }

    [RegisterPacketProcessor]
    internal class NC_UXA_PacketProcessor : BasePacketProcessor<NC_UXA_Packet>
    {
        public override void ProcessPacket(NC_UXA_Packet packet, INebulaConnection conn)
        {
            Log.Debug(packet.Type);
            if (packet.Type == NC_UXA_Packet.EType.InitDysonSphere)
            {
                if (IsHost)
                {
                    Multiplayer.Session.Server.SendPacketExclude(packet, conn);
                }
                var starIndex = (int)packet.Value1;
                var layerId = (int)packet.Value2;
                var ds = GameMain.data.dysonSpheres[starIndex];
                if (ds == null) return;
                if (layerId < 0)
                {
                    var dysonSphere = new DysonSphere();
                    GameMain.data.dysonSpheres[starIndex] = dysonSphere;
                    dysonSphere.Init(GameMain.data, GameMain.data.galaxy.stars[starIndex]);
                    dysonSphere.ResetNew();
                    return;
                }
                
                if (ds?.layersIdBased[layerId] == null) return;
                var pool = ds.rocketPool;
                for (var id = ds.rocketCursor - 1; id > 0; id--)
                {
                    if (pool[id].id != id) continue;
                    if (pool[id].nodeLayerId != layerId) continue;
                    ds.RemoveDysonRocket(id);
                }
                ds.RemoveLayer(layerId);
                return;
            }

            var factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null) return;
            using (Multiplayer.Session.Factories.IsIncomingRequest.On())
            {
                Multiplayer.Session.Factories.TargetPlanet = packet.PlanetId;
                Multiplayer.Session.Factories.PacketAuthor = packet.AuthorId;
                Multiplayer.Session.Factories.EventFactory = factory;
                if (!factory.planet.factoryLoaded)
                {
                    FactoryManager factoryManager = Multiplayer.Session.Factories as FactoryManager;
                    if (factoryManager.RemovePlanetTimer(packet.PlanetId))
                    {
                        factoryManager.UnloadPlanetData(packet.PlanetId);
                    }
                }

                switch (packet.Type)
                {
                    case NC_UXA_Packet.EType.DismantleAll:
                        foreach (var etd in factory.entityPool)
                        {
                            if (etd.id == 0) continue;
                            var stationId = etd.stationId;
                            if (stationId > 0)
                            {
                                // Clean up station storage first to avoid putting into player package
                                var sc = factory.transport.stationPool[stationId];
                                if (sc.storage != null)
                                {
                                    for (var i = 0; i < sc.storage.Length; i++)
                                    {
                                        sc.storage[i].count = 0;
                                    }
                                }
                            }
                            factory.RemoveEntityWithComponents(etd.id, false);
                        }
                        break;

                    case NC_UXA_Packet.EType.BuildOrbitalCollector:
                        var pos = new Vector3(packet.Value1, packet.Value2, packet.Value3);
                        var rot = Maths.SphericalRotation(pos, 0f);
                        var prebuild = new PrebuildData
                        {
                            protoId = 2105,
                            modelIndex = 117,
                            pos = pos,
                            pos2 = pos,
                            rot = rot,
                            rot2 = rot,
                            pickOffset = 0,
                            insertOffset = 0,
                            recipeId = 0,
                            filterId = 0,
                            paramCount = 0
                        };
                        factory.AddPrebuildDataWithComponents(prebuild);
                        break;
                }

                Multiplayer.Session.Factories.TargetPlanet = NebulaModAPI.PLANET_NONE;
                Multiplayer.Session.Factories.PacketAuthor = NebulaModAPI.AUTHOR_NONE;
                Multiplayer.Session.Factories.EventFactory = null;
            }
        }
    }
}
