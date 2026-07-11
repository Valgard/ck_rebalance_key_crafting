using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;

namespace RebalanceKeyCrafting
{
    /// <summary>
    /// Mod bootstrap. The Pugstorm mod loader instantiates this class on game
    /// start and calls the IMod lifecycle methods. The Harmony patch class is
    /// auto-discovered by the loader — there is no PatchAll() call. The patch
    /// target (PugDatabasePostConverter.PostConvert) is managed, not Burst, so
    /// no BurstDisabler call is needed.
    /// </summary>
    public sealed class RebalanceKeyCraftingMod : IMod
    {
        public void EarlyInit() { }

        public void Init()
        {
            // Register the key-crafting settings; ModConfig reads these live handles (the next bake
            // uses the current values — see the bake-time note in ModConfig). Section uses the default
            // AsDeclared sort, so builder-call order IS render order: enabled, cost, scope.
            ModSettings.Section(this)
                .Hint("Cheaper key crafting - how cheap, and which keys. Changes apply on restart.")
                .Toggle(out var en, "enabled", true)
                .Choice(out var reduction, "reductionFactor",
                    new[] { ModConfig.Reduction.OneIngot, ModConfig.Reduction.Quarter, ModConfig.Reduction.Half, ModConfig.Reduction.Vanilla },
                    ModConfig.Reduction.Quarter)
                .Choice(out var scope, "scope",
                    new[] { ModConfig.Scope.TierKeysOnly, ModConfig.Scope.AllCraftableKeys },
                    ModConfig.Scope.TierKeysOnly)
                .Build();

            var c = ModConfig.Instance;
            c.Bind(en, reduction, scope);
            Debug.Log(
                $"[RebalanceKeyCrafting] Mod initialized. enabled={c.enabled}, " +
                $"reductionFactor={c.reductionFactor}, minPerIngredient={c.minPerIngredient}, " +
                $"scope={c.scope}");
        }

        public void ModObjectLoaded(Object obj) { }
        public void Shutdown() { }
        public void Update() { }
    }
}
