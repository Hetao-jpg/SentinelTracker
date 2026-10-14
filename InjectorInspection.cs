using System;
using Il2Cpp;
using MelonLoader;

namespace SentinelTracker;

/// <summary>
/// 精确判定一支注射器是不是“真品”。
///
/// 判定路径（与原版 ConsumeImmunivax 完全一致）：
///   GameItem.FindItemFeatureByID("CATEGORY_GENUINE_INJECTOR", false)
///     → ItemFeature.realCondition      (dump.cs +0x60, interop 属性名 realCondition)
///       → ItemCondition.identifier     (dump.cs +0x18, interop 属性名 identifier)
///         == "genuineInjector"
/// </summary>
internal static class InjectorInspection
{
    private const string FEATURE_CATEGORY = "CATEGORY_GENUINE_INJECTOR";

    public const string SUBTYPE_GENUINE = "genuineInjector";
    public const string SUBTYPE_EXPIRED = "expiredInjector";
    public const string SUBTYPE_COUNTERFEIT = "counterfeitInjector";

    /// <summary>
    /// 读该注射器的 subtype 字符串。
    /// 返回 null 表示：物品为 null / 无 CATEGORY_GENUINE_INJECTOR 特征 / realCondition 为空 / 读异常。
    /// </summary>
    public static string ReadSubtype(GameItem item)
    {
        if (item == null) return null;
        try
        {
            var feature = item.FindItemFeatureByID(FEATURE_CATEGORY);
            if (feature == null) return null;

            var cond = feature.realCondition;   // interop 属性，+0x60
            if (cond == null) return null;

            string id = cond.identifier;        // interop 属性，+0x18
            return string.IsNullOrEmpty(id) ? null : id;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("[SentinelTracker] 读 ItemCondition 失败: " + ex.Message);
            return null;
        }
    }

    public static bool? IsGenuine(GameItem item)
    {
        string s = ReadSubtype(item);
        if (s == null) return null;
        return s == SUBTYPE_GENUINE;
    }

    /// <summary>
    /// 结合 days 变化做最终判定：
    ///   优先 subtype 精确判定；
    ///   读不到 subtype 时，用“days 从非 0 变 0”兜底（真品唯一会把 days 归零）。
    /// </summary>
    public static bool IsGenuineWithFallback(GameItem item, int prevDays, int curDays)
    {
        var r = IsGenuine(item);
        if (r.HasValue) return r.Value;
        return prevDays != 0 && curDays == 0;
    }
}