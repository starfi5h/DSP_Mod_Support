using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_GB_Packet
    {
        public EType Type { get; set; }
        public int PlanetId { get; set; }
        public int OrbitId { get; set; }
        public byte[] Data { get; set; }

        public NC_GB_Packet() { }
        public NC_GB_Packet(EType type, int planetId, int orbitId, byte[] data)
        {
            Type = type;
            PlanetId = planetId;
            OrbitId = orbitId;
            Data = data;
        }


        public enum EType
        {
            None,
            StroageBoxRequest,
            StroageBoxResponse
        }
    }

    [RegisterPacketProcessor]
    internal class NC_GB_PacketProcessor : BasePacketProcessor<NC_GB_Packet>
    {
        public override void ProcessPacket(NC_GB_Packet packet, INebulaConnection conn)
        {
            Log.Debug(packet.Type + " p"  + packet.PlanetId + " o" + packet.OrbitId);
            var factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null) return;

            switch (packet.Type)
            {
                case NC_GB_Packet.EType.StroageBoxRequest:
                {
                    using var p = NebulaModAPI.GetBinaryWriter();
                    ProjectGenesis.Patches.Logic.QuantumStorage.QuantumStoragePatches._components[packet.OrbitId - 1].Export(p.BinaryWriter);
                    packet.Data = p.CloseAndGetBytes();
                    packet.Type = NC_GB_Packet.EType.StroageBoxResponse;
                    conn.SendPacket(packet);
                    return;
                }
                case NC_GB_Packet.EType.StroageBoxResponse:
                {
                    using var p = NebulaModAPI.GetBinaryReader(packet.Data);
                    var storageComponent = ProjectGenesis.Patches.Logic.QuantumStorage.QuantumStoragePatches._components[packet.OrbitId - 1];
                    storageComponent.Import(p.BinaryReader);

                    // OnStorageIdChange()
                    var window = UIRoot.instance.uiGame.storageWindow;
                    if (window.active && window.factory != null && window.factoryStorage.storagePool[window.storageId] == storageComponent)
                    {
                        Log.Debug("Refresh Quantum depot window");
                        window.eventLock = true;
                        window.storageUI.OnStorageDataChanged();
                        window.bansSlider.maxValue = storageComponent.size;
                        window.bansSlider.value = (storageComponent.size - storageComponent.bans);
                        window.bansValueText.text = window.bansSlider.value.ToString();
                        window.eventLock = false;
                    }
                    return;
                }
            }
        }
    }
}
