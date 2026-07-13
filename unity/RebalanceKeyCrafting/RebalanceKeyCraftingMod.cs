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
        public void EarlyInit()
        {
            // Register + bind the key-crafting settings in EARLYINIT, not Init. The recipe rewrite runs
            // in PugDatabasePostConverter.PostConvert during Core Keeper's world/database conversion,
            // which the game performs AFTER EarlyInit but BEFORE Init. Binding here means the
            // SettingHandles already hold the persisted config values by the time the bake reads them
            // (ModConfig's getters read the live handle). Section uses the default AsDeclared sort, so
            // builder-call order IS render order: enabled, cost, scope. Every knob is bake-time
            // (idempotent PostConvert), so each is marked RequiresRestart: changing one and leaving the
            // Mod Settings menu raises CK's "restart to apply" prompt — the next launch's bake reads it.
            ModSettings.Section(this)
                .Hint("Cheaper key crafting - how cheap, and which keys. Changes apply on restart.")
                .Toggle(out var en, "enabled", true).RequiresRestart()
                .Choice(out var reduction, "reductionFactor",
                    new[] { ModConfig.Reduction.OneIngot, ModConfig.Reduction.Quarter, ModConfig.Reduction.Half, ModConfig.Reduction.Vanilla },
                    ModConfig.Reduction.Quarter).RequiresRestart()
                .Choice(out var scope, "scope",
                    new[] { ModConfig.Scope.TierKeysOnly, ModConfig.Scope.AllCraftableKeys },
                    ModConfig.Scope.TierKeysOnly).RequiresRestart()
                .Build();

            var c = ModConfig.Instance;
            c.Bind(en, reduction, scope);
            Debug.Log(
                $"[RebalanceKeyCrafting] Settings bound in EarlyInit. enabled={c.enabled}, " +
                $"reductionFactor={c.reductionFactor}, minPerIngredient={c.minPerIngredient}, " +
                $"scope={c.scope}");
        }

        public void Init() { }

        public void ModObjectLoaded(Object obj) { }
        public void Shutdown() { }
        public void Update() { }
    }
}
