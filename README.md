# Androids Don't Count

RimWorld 1.7 mod for [ChJees Androids](https://steamcommunity.com/sharedfiles/filedetails/?id=2889326293).
Androids remain fully functional colonists — they work, take jobs, can be selected and drafted, and
**still appear on the colonist bar** — but they are **excluded from storyteller raid/threat point
math and colony wealth** (including their own market value and everything worn or installed on them).

## What this mod changes

All android identification is exact-match only: a `DefModExtension` of type
`Androids.MechanicalPawnProperties` on the pawn's race def, or the `ChjAndroidLike` hediff.

1. **Pawn wealth** — transpiler on `RimWorld.WealthWatcher.ForceRecount(bool)` filters the
   `MapPawns.PawnsInFaction(Faction.OfPlayer)` loop so androids contribute no market value
   (which natively includes installed bionics/implants) and no health points.
2. **Gear wealth** — postfix on `RimWorld.WealthWatcher.WealthItemsFilter(IThingHolder)` returns
   `false` for android pawns and android corpses, so the recursive wealth traversal skips their
   worn apparel, primary weapons, belts, and inventory entirely.
3. **Encounter-map wealth** — prefix on
   `RimWorld.WealthWatcher.GetEquipmentApparelAndInventoryWealth(Pawn)` returns 0 for androids
   (used by `PlayerWealthForStoryteller` on non-home maps).
4. **Caravan wealth** — prefix on
   `RimWorld.Planet.Caravan.get_PlayerWealthForStoryteller` skips androids and their gear.
5. **Raid/threat points** — transpiler on
   `RimWorld.StorytellerUtility.DefaultThreatPointsNow(IIncidentTarget)` filters
   `target.PlayerPawnsForStoryteller`, so androids generate no colonist-curve or combat-power
   threat points. Combined with (1)–(4), they are excluded from both the wealth and colonist
   count paths of raid scaling.

## What this mod deliberately does NOT change

- **Colonist bar** — androids still appear on the bar like any other colonist
  (the v1.0.x bar filter was removed in v1.1.0 by request).
- `PawnUtility.IsFreeColonist` is never patched, so alerts, work assignment, tutor, and mood
  all continue to treat androids normally.
- Selection, drafting, and combat commands are untouched.
- Storyteller population intent (`StorytellerComp_FactionInteraction`, `MapPawns.ColonistCount`)
  still sees androids, so wanderer/joiner rates are unaffected.
- Ideoligion role thresholds and research are unaffected.
- Expectations drop indirectly because colony wealth drops.

## Build and test

```
dotnet build AndroidsDontCount.csproj -c Release -p:GameManagedDir=/path/to/RimWorldWin64_Data/Managed
dotnet run --project tests/AndroidsDontCount.Tests.csproj -c Release
```

`GameManagedDir` defaults to `/root/rimworld_libs`. Dependencies: Harmony, ChJees.Androids14.
Regression tests cover patch registration on all five wealth/threat targets, the android
classifier, transpiler structure, and null safety. **Not tested in a live game.**
