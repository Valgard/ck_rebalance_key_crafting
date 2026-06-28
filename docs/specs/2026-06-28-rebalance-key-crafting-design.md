# Rebalance Key Crafting — Design

- **Date:** 2026-06-28
- **Status:** Approved (pending spec review)
- **Mod identity:** repo `rebalance-key-crafting` · namespace `RebalanceKeyCrafting` · displayName "Rebalance Key Crafting"

## Summary

A Core Keeper mod that **reduces the material cost of crafting keys** by
rewriting their recipe ingredient amounts **at database bake time**, before
the recipe is sealed into the game's immutable runtime blob. Shipping default:
the seven tier chest keys (Copper → Relucite) at **×0.25** of their vanilla
ingredient amounts, with a floor of **1 per ingredient**. Standalone Harmony
patch — no CoreLib and no sibling-mod dependency.

## Goals

- Reduce the per-ingredient crafting cost of keys to a configurable fraction.
- **WYSIWYG:** the workbench UI shows the reduced cost *and* the actual
  consumption matches it — one source of truth, no display/deduction mismatch.
- **Extensible scope:** ship covering the 7 tier keys; switchable to "all
  craftable keys" via a single config flag without code surgery.

## Non-Goals

- No new items, recipes, or workbenches.
- No change to crafting *time*, key drop rates, or the locked-chest tiers.
- No runtime config file — Pugstorm's RoslynCSharp sandbox blocks `System.IO`,
  so configuration is a hardcoded `ModConfig` singleton (the sibling mods'
  established pattern).

## Background / findings

### Key items

Nine `ObjectID` enum values contain "Key" (`Pug.Base.decompiled.cs`):

| Key | ObjectID | Notes |
|-----|---------:|-------|
| CopperKey   | 212 | tier chest key |
| IronKey     | 215 | tier chest key |
| ScarletKey  | 218 | tier chest key |
| OctarineKey | 221 | tier chest key |
| GalaxiteKey | 224 | tier chest key |
| SolariteKey | 227 | tier chest key |
| ReluciteKey | 952 | tier chest key |
| PuzzleDoorKey    | 4804 | likely world-found progression gate |
| ProtocolSlateKey | 8253 | likely world-found progression gate |

Shipping scope is the **7 tier keys**. The two special keys are excluded by
default; they are reachable later via the `AllCraftableKeys` scope, which is
self-filtering (see *Target selection*) so found-only keys with empty recipes
are skipped automatically.

### Where recipe cost lives

- **Authoring (mutable):** `ObjectInfo.requiredObjectsToCraft` — a
  `List<CraftingObject>`, and `CraftingObject` is a **class** (reference type)
  with a public `int amount` (`Pug.Base.decompiled.cs`).
- **Runtime (immutable):** baked into `BlobArray<ObjectWithAmount>` consumed by
  the Burst-compiled crafting path (`InventoryUpdateSystem` →
  `ProcessCraftingJob` → `InventoryUtility.Craft`).

### Why bake-time rewrite (chosen approach)

The bake is performed by `PugDatabasePostConverter.PostConvert(GameObject
authoring)` (`Pug.Other.decompiled.cs:3478-3579`). It is **managed, not Burst**
(it manipulates `List<>`, `GameObject`, `BlobBuilder`, `EntityManager`), so it
is cleanly Harmony-patchable with no `BurstDisabler`. The decisive line:

```
Pug.Other.decompiled.cs:3547
blobBuilderArray2[j].amount = objectInfo2.requiredObjectsToCraft[j].amount;
```

The blob amount is copied **straight from the mutable List**. A Prefix that
scales `CraftingObject.amount` on the target keys *before* this loop runs makes
vanilla bake the reduced values into the blob. Because the same blob backs both
the UI cost display and the runtime consumption, the result is WYSIWYG with no
Burst patch, no per-craft overhead, and the work happens once per bake.

Rejected alternatives:

- **Postfix `PugDatabase.GetRequiredObjectsToCraft`** — returns
  `ref BlobArray<…>` (ref-returning method, effectively unpatchable by Harmony)
  and is called from Burst context.
- **In-place blob mutation after load** — writing into "immutable" BlobArray
  memory works but is off-label and fragile.
- **Runtime consume patch (`Craft`)** — UI still shows full cost (mismatch),
  needs a transpiler for the complex signature, and de-Bursting the central
  `InventoryUpdateSystem` is a disproportionately broad footprint.

## Architecture

Three runtime classes in the `RebalanceKeyCrafting` namespace, plus the shared
editor helpers symlinked in from `../utils/` (the sibling-mod convention).

- **`RebalanceKeyCraftingMod` (`IMod`)** — bootstrap. Logs the resolved config
  in `Init()`. **No `BurstDisabler`** — the patch target is managed.
- **`KeyRecipeCostPatch`** — auto-discovered `[HarmonyPatch(typeof(
  PugDatabasePostConverter), nameof(PugDatabasePostConverter.PostConvert))]`
  `Prefix(GameObject authoring)`. Resolves the prefab list, selects target
  keys, scales each recipe ingredient, guards idempotency. Returns `true`
  (always lets vanilla run).
- **`ModConfig`** — hardcoded singleton: `enabled`, `reductionFactor = 0.25f`,
  `minPerIngredient = 1`, `scope = Scope.TierKeysOnly`, and the
  `tierKeyIds` set. Singleton shape preserved so a future sandbox-safe loader
  (`API.ConfigFilesystem`) could drop in without touching the patch.

### Prefix logic

```
Prefix(GameObject authoring):
  if (!ModConfig.Instance.enabled) return true
  if (!authoring.TryGetComponent<PugDatabaseAuthoring>(out var comp)) return true
  foreach (PrefabData pd in DatabaseConversionUtility.GetPrefabList(comp)):
    ObjectInfo oi = pd.ObjectInfo
    if (!IsTargetKey(oi)) continue
    foreach (CraftingObject co in oi.requiredObjectsToCraft):
      if (!reduced.Add(co)) continue          // idempotency: skip if seen
      co.amount = max(minPerIngredient,
                      round(co.amount * reductionFactor))
  return true
```

### Reduction formula

`newAmount = max(minPerIngredient, round(oldAmount × reductionFactor))`

- Floor at `minPerIngredient` (= 1) prevents accidentally free crafting.
- `reductionFactor < 1`, so the value can only drop, never rise.
- **Rounding note (decide in planning):** C# `Math.Round` defaults to
  banker's rounding (half-to-even). With ×0.25 the only ties are small
  (e.g. 2 → 0.5 → 0 → floored to 1; 6 → 1.5 → 2). Behaviour is acceptable
  either way; the plan should pin the exact rounding mode explicitly.

### Idempotency

`PostConvert` runs once **per world conversion**, but the `ObjectInfo` /
`CraftingObject` instances persist on the prefabs across world loads within a
session. Without a guard, a second world load would scale the *already-reduced*
amounts again. Guard: a `static HashSet<CraftingObject>` keyed by reference;
each ingredient instance is reduced **exactly once** regardless of how many
times `PostConvert` fires.

### Target selection

- `TierKeysOnly` → `oi.objectID ∈ tierKeyIds` (the 7 IDs above).
- `AllCraftableKeys` → `oi.objectID.ToString()` contains "Key"
  (case-insensitive) **AND** `oi.requiredObjectsToCraft` is non-empty. The
  non-empty-recipe test makes found-only keys (no recipe) no-ops automatically,
  so no ID denylist is needed.

## Deployment / identity

- **Standalone:** no CoreLib, no sibling dependency. `requiredOn: 3`
  (ClientAndServer) so the reduction is consistent on both the UI side (client
  bake) and the consumption side (server bake).
- New independent git repo with a `unity/` mirror of the SDK `Assets/` tree;
  shared `../utils/` build/publish scripts; `utils/link.sh` symlinks.
- **Fake mod.io dev ID `9999992`** — next free descending (9999993 =
  reusable-cattle-box, 9999994 = faster-pet-talents, … 9999999 =
  disable-durability are taken; IDs must differ).

## Verification

Manual in-game smoke test (no automated tests in this mod family):

1. Open a tier-key recipe at its workbench → displayed ingredient cost is ×0.25
   of vanilla (min 1).
2. Craft the key → exactly that reduced amount is consumed (UI == deduction).
3. Reload the world (or load a second world) → cost does **not** drop further
   (idempotency holds).
4. Spot-check a non-key recipe is unchanged.

## Open risks (resolve during planning)

- Confirm which game DLL exports `PugDatabasePostConverter` /
  `DatabaseConversionUtility` and that the wizard's default asmdef reference
  set covers it (very likely; verify).
- Empirically confirm the 7 tier keys have a **non-empty** recipe (a found-only
  key would be a silent no-op) — visible immediately in the smoke test.
- Confirm `PostConvert` fires in both the client and server worlds for the
  relevant host topologies (solo host, dedicated server).
- Pin the exact rounding mode for the reduction formula.
