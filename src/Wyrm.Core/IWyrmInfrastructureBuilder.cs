namespace Wyrm.Events.Builder
{
    public interface IWyrmInfrastructureBuilder
    {
        void AddOrUpdateExtension<TExtension>(TExtension extension)
            where TExtension : class, IWyrmOptionsExtension;

    }
}