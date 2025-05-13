namespace DeeOkSeed33.DI
{
    public static class GlobalInjector
    {
        private static DIContainer _diContainer;

        public static void InjectAt(object target)
        {
            _diContainer.InjectAt(target);
        }
    }
}