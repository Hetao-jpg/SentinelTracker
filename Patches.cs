using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace SentinelTracker;

[HarmonyPatch(typeof(HealthData), nameof(HealthData.ConsumeImmunivax))]
internal static class ConsumeImmunivaxPatch
{
    [HarmonyPrefix]
    private static void Prefix(HealthData __instance, out int __state)
    {
        __state = __instance.daysSinceSentinelInjection;
    }

    [HarmonyPostfix]
    private static void Postfix(HealthData __instance, int __state, GameItem gameItem)
    {
        if (Core.CfgReducePoolOnGenuineInject == null ||
            !Core.CfgReducePoolOnGenuineInject.Value)
            return;

        bool isGenuine = InjectorInspection.IsGenuineWithFallback(
            gameItem, __state, __instance.daysSinceSentinelInjection);

        if (!isGenuine) return;

        int cur = __instance.sentinelPool;
        if (cur <= 0)
        {
            MelonLogger.Msg("[SentinelTracker] 真品免疫宁生效，但损伤池已为 0");
            return;
        }

        __instance.sentinelPool = cur - 1;
        MelonLogger.Msg($"[SentinelTracker] 真品免疫宁生效：损伤池 {cur} → {cur - 1}");
    }
}