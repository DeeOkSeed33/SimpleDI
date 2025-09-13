namespace DeeOkSeed33.DI
{
    public interface IPreInitializable
    {
        void PreInitialize();
    }
    
    public interface IInitializable
    {
        void Initialize();
    }

    public interface IPostInitializable
    {
        void PostInitialize();
    }
}