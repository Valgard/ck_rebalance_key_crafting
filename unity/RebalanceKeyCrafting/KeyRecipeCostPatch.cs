using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RebalanceKeyCrafting
{
    /// <summary>
    /// Reduces the material cost of crafting keys by rewriting their recipe
    /// ingredient amounts at database bake time. PugDatabasePostConverter.
    /// PostConvert copies each amount straight from the mutable
    /// ObjectInfo.requiredObjectsToCraft list into the immutable runtime blob
    /// ("blob.amount = objectInfo.requiredObjectsToCraft[j].amount"). Scaling
    /// the list in this Prefix makes vanilla bake the reduced values, so the
    /// workbench UI and the runtime consumption both see the lower cost. The
    /// target is managed (not Burst), so no BurstDisabler is required.
    /// </summary>
    [HarmonyPatch(typeof(PugDatabasePostConverter), nameof(PugDatabasePostConverter.PostConvert))]
    internal static class KeyRecipeCostPatch
    {
        // Idempotency: PostConvert runs once per world conversion, but the
        // ObjectInfo instances persist on the prefabs across world loads. Each
        // ObjectInfo's recipe is reduced exactly once, ever.
        private static readonly HashSet<ObjectInfo> _processed = new HashSet<ObjectInfo>();

        static KeyRecipeCostPatch()
        {
            Debug.Log("[RebalanceKeyCrafting] KeyRecipeCostPatch loaded.");
        }

        [HarmonyPrefix]
        private static bool Prefix(GameObject authoring)
        {
            var config = ModConfig.Instance;
            if (!config.enabled)
                return true;
            if (authoring == null)
                return true;
            if (!authoring.TryGetComponent<PugDatabaseAuthoring>(out _))
                return true;

            // 1.3 removed DatabaseConversionUtility and emptied PugDatabaseAuthoring, which now
            // only marks the object. Vanilla's own PostConvert reads the prefabs from
            // ScriptableData instead (Pug.Other:3513), so this walks the same source it does.
            var blocks = ScriptableData.GetDataBlocks<EntityAuthoringDataBlock>();
            if (blocks == null)
                return true;

            int recipesChanged = 0;
            foreach (var block in blocks)
            {
                var prefab = block.prefab;
                if (prefab == null)
                    continue;
                var entityData = prefab.GetComponent<IEntityMonoBehaviourData>();
                if (entityData == null)
                    continue;
                var info = entityData.ObjectInfo;
                if (info == null)
                    continue;
                if (info.requiredObjectsToCraft == null || info.requiredObjectsToCraft.Count == 0)
                    continue;
                if (!IsTargetKey(config, info))
                    continue;
                if (!_processed.Add(info))
                    continue; // already reduced this instance

                for (int i = 0; i < info.requiredObjectsToCraft.Count; i++)
                {
                    var ingredient = info.requiredObjectsToCraft[i]; // CraftingObject (class) — left unnamed on purpose
                    int reduced = Math.Max(
                        config.minPerIngredient,
                        (int)Math.Round(ingredient.amount * (double)config.reductionFactor, MidpointRounding.AwayFromZero)
                    );
                    ingredient.amount = reduced;
                }
                recipesChanged++;
            }

            if (recipesChanged > 0)
                Debug.Log($"[RebalanceKeyCrafting] Reduced {recipesChanged} key recipe(s) " + $"(scope={config.scope}, factor={config.reductionFactor}).");
            return true; // always run the vanilla bake
        }

        private static bool IsTargetKey(ModConfig config, ObjectInfo info)
        {
            switch (config.scope)
            {
                case ModConfig.Scope.TierKeysOnly:
                    return config.tierKeyIds.Contains(info.objectID);
                case ModConfig.Scope.AllCraftableKeys:
                    // Non-empty recipe is already checked by the caller, so
                    // found-only keys are excluded automatically.
                    return info.objectID.ToString().EndsWith("Key", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
}
