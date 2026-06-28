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
            var c = ModConfig.Instance;
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
