namespace RateMonitor.Model.Processor
{
    public static class EntityProcessorManager
    {
        static readonly AssemblerProcessor assemblerProcessor = new();
        static readonly EjectorProcessor ejectorProcessor = new();
        static readonly FractionatorProcessor fractionatorProcessor = new();
        static readonly LabProcessor labProcessor = new();
        static readonly MinerProcessor minerProcessor = new();
        static readonly NoneProcessor noneProcessor = new();
        static readonly PowerExcProcessor powerExcProcessor = new();
        static readonly PowerGenProcessor powerGenProcessor = new();
        static readonly SiloProcessor siloProcessor = new();
        static readonly SpraycoaterProcessor spraycoaterProcessor = new();

        public static IEntityProcessor GetProcessor(in EntityData entityData)
        {
            if (entityData.assemblerId > 0) return assemblerProcessor;
            if (entityData.labId > 0) return labProcessor;
            if (entityData.minerId > 0) return minerProcessor;
            if (entityData.fractionatorId > 0) return fractionatorProcessor;
            if (entityData.ejectorId > 0) return ejectorProcessor;
            if (entityData.siloId > 0) return siloProcessor;
            if (entityData.powerGenId > 0) return powerGenProcessor;
            if (entityData.powerExcId > 0) return powerExcProcessor;
            if (entityData.spraycoaterId > 0) return spraycoaterProcessor;

            return noneProcessor;
        }
    }
}
