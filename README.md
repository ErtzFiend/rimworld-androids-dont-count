# Androids Don't Count

RimWorld 1.7 mod for [ChJees Androids](https://steamcommunity.com/sharedfiles/filedetails/?id=2889326293).
Androids remain fully functional colonists — they work, take jobs, can be selected and drafted —
but they are **excluded from the colonist bar count, storyteller raid/threat point math, and
colony wealth** (including their own market value and everything worn or installed on them).

## What this mod changes

All android identification is exact-match only: a `DefModExtension` of type
`Androids.MechanicalPawnProperties` on the pawn's race def, or the `ChjAndroidLike` hediff.

1. **Colonist bar** — postfix on `RimWorld.ColonistBar.CheckRecacheEntries` filters android
   pawns out of `cachedEntries` and recalculates layout via
   `ColonistBarDrawLocsFinder.CalculateDrawLocs`. Deliberately does **not** patch
   `PawnUtility.IsFreeColonist`, which would leak into alerts, work assignment, tutor, and mood.
   Caravan groups and multi-colony layout are preserved.
2. **Pawn wealth** — transpiler on `RimWorld.WealthWatcher.ForceRecount(bool)` filters the
   `MapPawns.PawnsInFaction(Faction.OfPlayer)` loop so androids contribute no market value
   (which natively includes installed bionics/implants) and no health points.
3. **Gear wealth** — postfix on `RimWorld.WealthWatcher.WealthItemsFilter(IThingHolder)` returns
   `false` for android pawns and android corpses, so the recursive wealth traversal skips their
   worn apparel, primary weapons, belts, and inventory entirely.
4. **Encounter-map wealth** — prefix on
   `RimWorld.WealthWatcher.GetEquipmentApparelAndInventoryWealth(Pawn)` returns 0 for androids
   (used by `PlayerWealthForStoryteller` on non-home maps).
5. **Caravan wealth** — prefix on
   `RimWorld.Planet.Caravan.get_PlayerWealthForStoryteller` skips androids and their gear.
6. **Raid/threat points** — transpiler on
   `RimWorld.StorytellerUtility.DefaultThreatPointsNow(IIncidentTarget)` filters
   `target.PlayerPawnsForStoryteller`, so androids generate no colonist-curve or combat-power
   threat points. Combined with (2)–(5), they are excluded from both the wealth and colonist
   count paths of raid scaling.

## What still counts androids (by design)

Only the bar, wealth, and raid math are patched. Everything else stays vanilla:
work tab and job drivers, alerts (`Need doctor`, `Colonists exhausted`, etc.), selection and
drafting, storyteller population intent (`StorytellerComp_FactionInteraction`,
`MapPawns.ColonistCount` — so wanderer/joiner rates are unaffected), ideoligion role
thresholds, and research. Expectations drop indirectly because colony wealth drops.

## Build and test

```
dotnet build AndroidsDontCount.csproj -c Release -p:GameManagedDir=/path/to/RimWorldWin64_Data/Managed
dotnet run --project tests/AndroidsDontCount.Tests.csproj -c Release
```

`GameManagedDir` defaults to `/root/rimworld_libs`. Dependencies: Harmony, ChJees.Androids14.
Regression tests (33 checks) cover patch registration on all six targets, the android
classifier, transpiler structure, and null safety. **Not tested in a live game.**
