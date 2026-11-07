using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using AndroidsDontCount;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Androids
{
    public class MechanicalPawnProperties : DefModExtension
    {
    }
}

internal static class Program
{
    private static int failures;

    private static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
        if (!ok) failures++;
    }

    private static int Main()
    {
        // Microsoft.Extensions.* 10.x runtime assembly fallback
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            var path = "/root/.nuget/packages/microsoft.aspnetcore.app.runtime.linux-x64/10.0.14/runtimes/linux-x64/lib/net10.0/" + name.Name + ".dll";
            return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
        };

        // 1. Initialize patches via static constructor
        RuntimeHelpers.RunClassConstructor(typeof(AndroidsDontCountMain).TypeHandle);


        // Target 2: WealthWatcher.ForceRecount
        var wwfrTarget = AccessTools.Method(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount));
        Check(wwfrTarget != null, "Target 2 resolves: WealthWatcher.ForceRecount");
        var wwfrInfo = Harmony.GetPatchInfo(wwfrTarget);
        bool wwfrRegistered = wwfrInfo != null && wwfrInfo.Transpilers.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_WealthWatcher_ForceRecount));
        Check(wwfrRegistered, "Target 2 registered: WealthWatcher.ForceRecount transpiler");

        // Target 3: WealthWatcher.WealthItemsFilter
        var wwifTarget = AccessTools.Method(typeof(WealthWatcher), nameof(WealthWatcher.WealthItemsFilter));
        Check(wwifTarget != null, "Target 3 resolves: WealthWatcher.WealthItemsFilter");
        var wwifInfo = Harmony.GetPatchInfo(wwifTarget);
        bool wwifRegistered = wwifInfo != null && wwifInfo.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_WealthWatcher_WealthItemsFilter));
        Check(wwifRegistered, "Target 3 registered: WealthWatcher.WealthItemsFilter postfix");

        // Target 4: WealthWatcher.GetEquipmentApparelAndInventoryWealth
        var geaiwTarget = AccessTools.Method(typeof(WealthWatcher), nameof(WealthWatcher.GetEquipmentApparelAndInventoryWealth));
        Check(geaiwTarget != null, "Target 4 resolves: WealthWatcher.GetEquipmentApparelAndInventoryWealth");
        var geaiwInfo = Harmony.GetPatchInfo(geaiwTarget);
        bool geaiwRegistered = geaiwInfo != null && geaiwInfo.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_WealthWatcher_GetEquipmentApparelAndInventoryWealth));
        Check(geaiwRegistered, "Target 4 registered: WealthWatcher.GetEquipmentApparelAndInventoryWealth prefix");

        // Target 5: Caravan.get_PlayerWealthForStoryteller
        var cwsTarget = AccessTools.PropertyGetter(typeof(Caravan), "PlayerWealthForStoryteller");
        Check(cwsTarget != null, "Target 5 resolves: Caravan.PlayerWealthForStoryteller");
        var cwsInfo = Harmony.GetPatchInfo(cwsTarget);
        bool cwsRegistered = cwsInfo != null && cwsInfo.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_Caravan_PlayerWealthForStoryteller));
        Check(cwsRegistered, "Target 5 registered: Caravan.PlayerWealthForStoryteller prefix");

        // Target 6: StorytellerUtility.DefaultThreatPointsNow
        var dtpnTarget = AccessTools.Method(typeof(StorytellerUtility), nameof(StorytellerUtility.DefaultThreatPointsNow));
        Check(dtpnTarget != null, "Target 6 resolves: StorytellerUtility.DefaultThreatPointsNow");
        var dtpnInfo = Harmony.GetPatchInfo(dtpnTarget);
        bool dtpnRegistered = dtpnInfo != null && dtpnInfo.Transpilers.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_StorytellerUtility_DefaultThreatPointsNow));
        Check(dtpnRegistered, "Target 6 registered: StorytellerUtility.DefaultThreatPointsNow transpiler");

        // Classifier tests
        Check(!AndroidClassifier.IsAndroid(null), "Classifier: null pawn is false");
        Check(!AndroidClassifier.IsAndroidCorpse(null), "Classifier: null corpse is false");

        // Create pawn with humanlike def
        var humanDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        humanDef.defName = "Human";
        var humanPawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        humanPawn.def = humanDef;
        Check(!AndroidClassifier.IsAndroid(humanPawn), "Classifier: human pawn is false");

        // Create android pawn via DefModExtension
        var androidDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        androidDef.defName = "ChjAndroid";
        androidDef.modExtensions = new List<DefModExtension> { new Androids.MechanicalPawnProperties() };
        var androidModExtPawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        androidModExtPawn.def = androidDef;
        Check(AndroidClassifier.IsAndroid(androidModExtPawn), "Classifier: pawn with MechanicalPawnProperties modExtension is true");

        var hediffDef = (HediffDef)RuntimeHelpers.GetUninitializedObject(typeof(HediffDef));
        hediffDef.defName = AndroidClassifier.AndroidLikeHediffDefName;
        var hediff = (Hediff)RuntimeHelpers.GetUninitializedObject(typeof(Hediff));
        hediff.def = hediffDef;

        var androidHediffPawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        androidHediffPawn.def = humanDef;
        var healthTracker = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));
        var hediffSet = (HediffSet)RuntimeHelpers.GetUninitializedObject(typeof(HediffSet));
        hediffSet.hediffs = new List<Hediff> { hediff };
        healthTracker.hediffSet = hediffSet;
        androidHediffPawn.health = healthTracker;
        Check(AndroidClassifier.IsAndroid(androidHediffPawn), "Classifier: pawn with ChjAndroidLike hediff is true");

        // Corpse tests
        var humanCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse));
        var androidCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse));

        var humanContainer = (ThingOwner<Pawn>)RuntimeHelpers.GetUninitializedObject(typeof(ThingOwner<Pawn>));
        var innerListField = AccessTools.Field(typeof(ThingOwner<Pawn>), "innerList");
        innerListField.SetValue(humanContainer, new List<Pawn> { humanPawn });
        AccessTools.Field(typeof(Corpse), "innerContainer").SetValue(humanCorpse, humanContainer);

        var androidContainer = (ThingOwner<Pawn>)RuntimeHelpers.GetUninitializedObject(typeof(ThingOwner<Pawn>));
        innerListField.SetValue(androidContainer, new List<Pawn> { androidHediffPawn });
        AccessTools.Field(typeof(Corpse), "innerContainer").SetValue(androidCorpse, androidContainer);

        Check(!AndroidClassifier.IsAndroidCorpse(humanCorpse), "Classifier: human corpse is false");
        Check(AndroidClassifier.IsAndroidCorpse(androidCorpse), "Classifier: android corpse is true");

        // Logic check: Patch_WealthWatcher_ForceRecount.FilterPawnsForWealth
        var pawnList = new List<Pawn> { humanPawn, androidHediffPawn };
        var filteredWealth = Patch_WealthWatcher_ForceRecount.FilterPawnsForWealth(pawnList);
        Check(filteredWealth.Count == 1 && filteredWealth[0] == humanPawn, "ForceRecount filter removes android pawns");
        Check(Patch_WealthWatcher_ForceRecount.FilterPawnsForWealth(null) == null, "ForceRecount filter handles null");

        // Logic check: Patch_WealthWatcher_WealthItemsFilter
        bool itemFilterResult = true;
        Patch_WealthWatcher_WealthItemsFilter.Postfix(humanPawn, ref itemFilterResult);
        Check(itemFilterResult == true, "WealthItemsFilter leaves human pawn true");
        Patch_WealthWatcher_WealthItemsFilter.Postfix(androidHediffPawn, ref itemFilterResult);
        Check(itemFilterResult == false, "WealthItemsFilter sets android pawn false");

        itemFilterResult = true;
        Patch_WealthWatcher_WealthItemsFilter.Postfix(humanCorpse, ref itemFilterResult);
        Check(itemFilterResult == true, "WealthItemsFilter leaves human corpse true");
        Patch_WealthWatcher_WealthItemsFilter.Postfix(androidCorpse, ref itemFilterResult);
        Check(itemFilterResult == false, "WealthItemsFilter sets android corpse false");

        // Logic check: Patch_WealthWatcher_GetEquipmentApparelAndInventoryWealth
        float wealthVal = 500f;
        bool runOrigWealth = Patch_WealthWatcher_GetEquipmentApparelAndInventoryWealth.Prefix(humanPawn, ref wealthVal);
        Check(runOrigWealth && wealthVal == 500f, "GetEquipmentApparelAndInventoryWealth runs original for human");
        runOrigWealth = Patch_WealthWatcher_GetEquipmentApparelAndInventoryWealth.Prefix(androidHediffPawn, ref wealthVal);
        Check(!runOrigWealth && wealthVal == 0f, "GetEquipmentApparelAndInventoryWealth returns 0 for android");

        // Logic check: Patch_StorytellerUtility_DefaultThreatPointsNow.FilterStorytellerPawns
        var filteredThreat = Patch_StorytellerUtility_DefaultThreatPointsNow.FilterStorytellerPawns(pawnList);
        var threatList = filteredThreat.ToList();
        Check(threatList.Count == 1 && threatList[0] == humanPawn, "DefaultThreatPointsNow filter removes android pawns");
        Check(Patch_StorytellerUtility_DefaultThreatPointsNow.FilterStorytellerPawns(null) == null, "DefaultThreatPointsNow filter handles null");

        // Logic check: Transpiler execution test on DefaultThreatPointsNow
        var dtpnInstructions = PatchProcessor.GetOriginalInstructions(dtpnTarget);
        var modifiedInstructions = Patch_StorytellerUtility_DefaultThreatPointsNow.Transpiler(dtpnInstructions).ToList();
        Check(modifiedInstructions.Count == dtpnInstructions.Count + 1, "Threat transpiler inserts exactly one filter instruction");

        // Logic check: Transpiler execution test on ForceRecount
        var frInstructions = PatchProcessor.GetOriginalInstructions(wwfrTarget);
        var frModified = Patch_WealthWatcher_ForceRecount.Transpiler(frInstructions).ToList();
        Check(frModified.Count == frInstructions.Count + 1, "Wealth transpiler inserts exactly one filter instruction");

        // Logic check: Patch_Caravan_PlayerWealthForStoryteller null / uncontrolled
        float caravanWealth = 100f;
        bool runCaravan = Patch_Caravan_PlayerWealthForStoryteller.Prefix(null, ref caravanWealth);
        Check(!runCaravan && caravanWealth == 0f, "Caravan wealth returns 0 for null caravan");


        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : failures + " TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }
}
