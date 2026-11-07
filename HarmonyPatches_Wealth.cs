using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AndroidsDontCount
{
    /// <summary>
    /// Excludes android pawns from the pawn wealth loop in WealthWatcher.ForceRecount.
    /// This prevents android market values (base value + installed bionics/implants)
    /// and android health scores from entering wealthPawns and totalHealth.
    /// </summary>
    [HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount))]
    public static class Patch_WealthWatcher_ForceRecount
    {
        public static List<Pawn> FilterPawnsForWealth(List<Pawn> pawns)
        {
            if (pawns == null) return null;
            var list = new List<Pawn>(pawns.Count);
            for (int i = 0; i < pawns.Count; i++)
            {
                var p = pawns[i];
                if (!AndroidClassifier.IsAndroid(p))
                {
                    list.Add(p);
                }
            }
            return list;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = AccessTools.Method(typeof(MapPawns), nameof(MapPawns.PawnsInFaction));
            var filterMethod = AccessTools.Method(typeof(Patch_WealthWatcher_ForceRecount), nameof(FilterPawnsForWealth));

            bool found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!found && instruction.Calls(targetMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, filterMethod);
                    found = true;
                }
            }

            if (!found)
            {
                Log.Error("[AndroidsDontCount] Failed to find MapPawns.PawnsInFaction in WealthWatcher.ForceRecount");
            }
        }
    }

    /// <summary>
    /// Filters out android pawns and corpses from ThingOwnerUtility container recursion in WealthWatcher.CalculateWealthItems.
    /// This ensures worn apparel, weapons, belts, and inventory items carried by androids are never added to wealthItems.
    /// </summary>
    [HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.WealthItemsFilter))]
    public static class Patch_WealthWatcher_WealthItemsFilter
    {
        public static void Postfix(IThingHolder x, ref bool __result)
        {
            if (!__result) return;
            if (x is Pawn p && AndroidClassifier.IsAndroid(p))
            {
                __result = false;
                return;
            }
            if (x is Corpse c && AndroidClassifier.IsAndroidCorpse(c))
            {
                __result = false;
                return;
            }
        }
    }

    /// <summary>
    /// Returns 0 equipment, apparel, and inventory wealth for android pawns.
    /// Used by Map.PlayerWealthForStoryteller (on non-home maps) and general wealth queries.
    /// </summary>
    [HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.GetEquipmentApparelAndInventoryWealth))]
    public static class Patch_WealthWatcher_GetEquipmentApparelAndInventoryWealth
    {
        public static bool Prefix(Pawn p, ref float __result)
        {
            if (AndroidClassifier.IsAndroid(p))
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Excludes android pawns' market value and gear wealth from caravan wealth calculations.
    /// </summary>
    [HarmonyPatch(typeof(Caravan), "get_PlayerWealthForStoryteller")]
    public static class Patch_Caravan_PlayerWealthForStoryteller
    {
        public static bool Prefix(Caravan __instance, ref float __result)
        {
            if (__instance == null || !__instance.IsPlayerControlled)
            {
                __result = 0f;
                return false;
            }

            float num = 0f;
            var pawns = __instance.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                var p = pawns[i];
                if (AndroidClassifier.IsAndroid(p)) continue;

                num += WealthWatcher.GetEquipmentApparelAndInventoryWealth(p);
                if (p.Faction == Faction.OfPlayer)
                {
                    float num2 = p.MarketValue;
                    if (p.IsSlave)
                    {
                        num2 *= 0.75f;
                    }
                    num += num2;
                }
            }

            __result = num * 0.7f;
            return false;
        }
    }
}
