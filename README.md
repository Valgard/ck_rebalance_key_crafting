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

Open **Options → Mod Settings** in-game to configure the mod; the Mod Settings
Menu framework persists your choices. Changes apply on the next game restart —
the menu offers to restart for you.

| Setting | Default | Meaning |
|---------|---------|---------|
| Enabled | On | Master toggle; when off, keys cost their vanilla amount. |
| Crafting cost | 1/4 | How cheap each key is — **1 ingot**, **1/4**, **1/2**, or **vanilla** (no reduction). A floor of 1 per ingredient always applies, so crafting never becomes free. |
| Affected keys | Tier keys | **Tier keys** (the 7 chest keys) or **all craftable keys**. |

## How it works

The reduction is a single Harmony prefix on `PugDatabasePostConverter.PostConvert`
that scales the keys' `requiredObjectsToCraft` amounts before vanilla bakes the
recipe into its immutable runtime blob. See `CLAUDE.md` for the full rationale.
