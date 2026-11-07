using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AndroidsDontCount
{
    /// <summary>
    /// Excludes android pawns from the colonist bar enumeration.
    /// This is the narrowest verified patch for the colonist bar:
    /// CheckRecacheEntries is the sole internal method that builds ColonistBar.cachedEntries.
    /// By pruning androids here and recalculating draw layouts, androids never appear on the bar,
    /// while leaving PawnUtility.IsFreeColonist and MapPawns.FreeColonists intact so alerts, jobs,
    /// work tabs, mood, and drafting continue to work normally.
    /// </summary>
    [HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
    public static class Patch_ColonistBar_CheckRecacheEntries
    {
        private static readonly AccessTools.FieldRef<ColonistBar, List<ColonistBar.Entry>> CachedEntriesRef =
            AccessTools.FieldRefAccess<ColonistBar, List<ColonistBar.Entry>>("cachedEntries");

        private static readonly AccessTools.FieldRef<ColonistBar, List<Vector2>> CachedDrawLocsRef =
            AccessTools.FieldRefAccess<ColonistBar, List<Vector2>>("cachedDrawLocs");

        private static readonly AccessTools.FieldRef<ColonistBar, List<int>> CachedReorderableGroupsRef =
            AccessTools.FieldRefAccess<ColonistBar, List<int>>("cachedReorderableGroups");

        private static readonly AccessTools.FieldRef<ColonistBar, float> CachedScaleRef =
            AccessTools.FieldRefAccess<ColonistBar, float>("cachedScale");

        private static readonly AccessTools.FieldRef<ColonistBar, ColonistBarColonistDrawer> DrawerRef =
            AccessTools.FieldRefAccess<ColonistBar, ColonistBarColonistDrawer>("drawer");

        private static readonly AccessTools.FieldRef<ColonistBar, ColonistBarDrawLocsFinder> DrawLocsFinderRef =
            AccessTools.FieldRefAccess<ColonistBar, ColonistBarDrawLocsFinder>("drawLocsFinder");

        public static void Postfix(ColonistBar __instance)
        {
            if (__instance == null) return;
            var entries = CachedEntriesRef(__instance);
            if (entries == null || entries.Count == 0) return;

            bool hasAndroids = false;
            for (int i = 0; i < entries.Count; i++)
            {
                var p = entries[i].pawn;
                if (p != null && AndroidClassifier.IsAndroid(p))
                {
                    hasAndroids = true;
                    break;
                }
            }
            if (!hasAndroids) return;

            FilterEntries(__instance, entries);
        }

        public static void FilterEntries(ColonistBar bar, List<ColonistBar.Entry> entries)
        {
            var cachedDrawLocs = CachedDrawLocsRef(bar);
            var cachedReorderableGroups = CachedReorderableGroupsRef(bar);
            var drawer = DrawerRef(bar);
            var drawLocsFinder = DrawLocsFinderRef(bar);

            var groupMaps = new Dictionary<int, Map>();
            var nonAndroidEntries = new List<ColonistBar.Entry>(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.map != null && !groupMaps.ContainsKey(entry.group))
                {
                    groupMaps[entry.group] = entry.map;
                }

                if (entry.pawn == null)
                {
                    // Existing empty-group placeholder
                    nonAndroidEntries.Add(entry);
                }
                else if (!AndroidClassifier.IsAndroid(entry.pawn))
                {
                    nonAndroidEntries.Add(entry);
                }
            }

            // Check if any map group became completely empty due to android pruning
            var remainingGroups = new HashSet<int>();
            for (int i = 0; i < nonAndroidEntries.Count; i++)
            {
                remainingGroups.Add(nonAndroidEntries[i].group);
            }

            foreach (var kvp in groupMaps)
            {
                if (!remainingGroups.Contains(kvp.Key))
                {
                    // Restore empty-map placeholder so map group frame remains intact
                    nonAndroidEntries.Add(new ColonistBar.Entry(null, kvp.Value, kvp.Key));
                }
            }

            // Map old group indices to consecutive 0, 1, 2...
            var oldToNewGroup = new Dictionary<int, int>();
            int nextNewGroup = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                int oldG = entries[i].group;
                if (!oldToNewGroup.ContainsKey(oldG))
                {
                    for (int j = 0; j < nonAndroidEntries.Count; j++)
                    {
                        if (nonAndroidEntries[j].group == oldG)
                        {
                            oldToNewGroup[oldG] = nextNewGroup++;
                            break;
                        }
                    }
                }
            }

            entries.Clear();
            for (int i = 0; i < nonAndroidEntries.Count; i++)
            {
                var e = nonAndroidEntries[i];
                int newGroup = oldToNewGroup.TryGetValue(e.group, out int mapped) ? mapped : 0;
                entries.Add(new ColonistBar.Entry(e.pawn, e.map, newGroup));
            }

            // Keep entries ordered by group
            entries.Sort((a, b) => a.group.CompareTo(b.group));

            // Resynchronize cached reorderable groups
            if (cachedReorderableGroups != null)
            {
                cachedReorderableGroups.Clear();
                for (int i = 0; i < entries.Count; i++)
                {
                    cachedReorderableGroups.Add(-1);
                }
            }

            drawer?.Notify_RecachedEntries();

            if (drawLocsFinder != null && cachedDrawLocs != null)
            {
                int groupsCount = nextNewGroup;
                drawLocsFinder.CalculateDrawLocs(cachedDrawLocs, out float newScale, groupsCount);
                CachedScaleRef(bar) = newScale;
            }
        }
    }
}
