namespace GTFOStats.Data
{
    public static class DanosPatchManager
    {
        public static List<DanosPatchConfiguration> GetPatchConfigurations()
        {
            // Example: Hard-coded patch configurations
            // In future, replace this with JSON or external config loading
            return new List<DanosPatchConfiguration>
            {
                new DanosPatchConfiguration
                {
                    TargetClass = "GTFuckingXP.Scripts.XpHandler",
                    TargetMethod = "AddXp",
                    PostfixMethod = "GTFOStats.Patches.XpHandlerPatches.AddXpPostfix"
                }
            };
        }
    }


}