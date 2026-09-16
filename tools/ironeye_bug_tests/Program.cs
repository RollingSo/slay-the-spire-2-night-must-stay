using HarmonyLib;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Patches;

string cardSource = File.ReadAllText(Path.Combine(
    Directory.GetCurrentDirectory(),
    "src", "Core", "Models", "Cards", "IroneyeCards78To86.cs"));
Check(
    cardSource.Contains("FromCardWithCardHoverTips<Approach>(IsUpgraded)")
    && cardSource.Contains("FromCardWithCardHoverTips<Retreat>(IsUpgraded)"),
    "Hundred Schemes previews are not tied to its upgrade state.");

var postfix = AccessTools.Method(
    typeof(IroneyeUnsettlingLampPatch),
    "KeepDistanceAtNormalMultiplier");
object[] distanceArgs = { new DistancePower(), 2m };
postfix.Invoke(null, distanceArgs);
Check((decimal)distanceArgs[1] == 1m, "Unsettling Lamp still doubles Distance.");

Console.WriteLine("PASS: Hundred Schemes previews upgraded outputs; Unsettling Lamp cannot double Distance-derived stat changes.");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
