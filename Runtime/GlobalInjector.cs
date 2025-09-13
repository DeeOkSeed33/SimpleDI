namespace DeeOkSeed33.DI
{
    public sealed class GlobalInjector
    {
        [Inject] private static DIContainer _diContainer;

        public static void InjectAt(object target)
        {
            _diContainer.InjectAt(target);
        }
    }
}