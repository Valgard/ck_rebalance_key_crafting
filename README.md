# Rebalance Key Crafting

A small Core Keeper mod that **reduces the material cost of crafting keys**.

By default it affects the seven tier chest keys — Copper, Iron, Scarlet,
Octarine, Galaxite, Solarite, and Relucite — lowering each recipe ingredient
to **25% of its vanilla amount** (never below 1). Both the workbench cost
display and the actual material consumption show the reduced cost: the mod
rewrites the recipe at database bake time, so there is no mismatch between what
you see and what you pay.

Personal-use, non-commercial (Pugstorm EULA).

## Install

- **mod.io:** subscribe to the mod; Core Keeper downloads it on next launch.
- **Local build:** see `CLAUDE.md` → *Build and deploy*.

## Configuration

There is no runtime config file (the mod sandbox blocks file I/O), so the two
knobs live in `unity/RebalanceKeyCrafting/ModConfig.cs` and take effect on the
next build:

| Field | Default | Meaning |
|-------|---------|---------|
| `reductionFactor` | `0.25f` | Each ingredient amount is multiplied by this (must be `< 1` to reduce). |
| `minPerIngredient` | `1` | Floor applied after scaling, so crafting never becomes free. |
| `scope` | `TierKeysOnly` | `TierKeysOnly` = the 7 tier chest keys; `AllCraftableKeys` = every craftable key. |

## How it works

The reduction is a single Harmony prefix on `PugDatabasePostConverter.PostConvert`
that scales the keys' `requiredObjectsToCraft` amounts before vanilla bakes the
recipe into its immutable runtime blob. See `CLAUDE.md` for the full rationale.
