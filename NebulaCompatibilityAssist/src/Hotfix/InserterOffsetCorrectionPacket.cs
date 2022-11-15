using NebulaAPI;
using System;

namespace NebulaCompatibilityAssist.Hotfix
{
    public class InserterOffsetCorrectionPacket
    {
        public int InserterId { get; set; }
        public short PickOffset { get; set; }
        public short InsertOffset { get; set; }
        public int PlanetId { get; set; }

        public InserterOffsetCorrectionPacket() { }
        public InserterOffsetCorrectionPacket(int inserterId, short pickOffset, short insertOffset, int planetId)
        {
            InserterId = inserterId;
            PickOffset = pickOffset;
            InsertOffset = insertOffset;
            PlanetId = planetId;
        }
    }

    [RegisterPacketProcessor]
    internal class InserterOffsetCorrectionProcessor : BasePacketProcessor<InserterOffsetCorrectionPacket>
    {
        public override void ProcessPacket(InserterOffsetCorrectionPacket packet, INebulaConnection conn)
        {
            InserterComponent[] pool = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory?.factorySystem?.inserterPool;
            if (pool != null)
            {
                Log.Warn($"{packet.PlanetId} Fix inserter{packet.InserterId} pickOffset->{packet.PickOffset} insertOffset->{packet.InsertOffset}");
                pool[packet.InserterId].pickOffset = packet.PickOffset;
                pool[packet.InserterId].insertOffset = packet.InsertOffset;
            }
        }
    }
}
