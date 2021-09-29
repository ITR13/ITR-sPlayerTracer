using VRChatUtilityKit.Utilities;

namespace VrcTracer
{
    // Mostly stolen from https://github.com/loukylor/VRC-Mods/blob/main/TriggerESP/TriggerESPMod.cs#L82-L89
    internal static class WorldCheck
    {
        public static void Init()
        {
            VRCUtils.OnEmmWorldCheckCompleted += areRiskyFuncsAllowed =>
            {
                MainClass.NotForceDisable = areRiskyFuncsAllowed;
            };
        }
    }
}