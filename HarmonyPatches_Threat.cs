using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidsDontCount
{
    /// <summary>
    /// Filters out android pawns from target.PlayerPawnsForStoryteller in StorytellerUtility.DefaultThreatPointsNow.
    /// This prevents android pawns from contributing colonist threat points or combat power to raid / incident points.
    /// </summary>
    [HarmonyPatch(typeof(StorytellerUtility), nameof(StorytellerUtility.DefaultThreatPointsNow))]
    public static class Patch_StorytellerUtility_DefaultThreatPointsNow
    {
        public static IEnumerable<Pawn> FilterStorytellerPawns(IEnumerable<Pawn> pawns)
        {
            if (pawns == null) return null;
            var list = new List<Pawn>();
            foreach (var p in pawns)
            {
                if (p != null && !AndroidClassifier.IsAndroid(p))
                {
                    list.Add(p);
                }
            }
            return list;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = AccessTools.PropertyGetter(typeof(IIncidentTarget), nameof(IIncidentTarget.PlayerPawnsForStoryteller));
            var filterMethod = AccessTools.Method(typeof(Patch_StorytellerUtility_DefaultThreatPointsNow), nameof(FilterStorytellerPawns));

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
                Log.Error("[AndroidsDontCount] Failed to find IIncidentTarget.PlayerPawnsForStoryteller in StorytellerUtility.DefaultThreatPointsNow");
            }
        }
    }
}
