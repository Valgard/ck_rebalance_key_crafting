using System.Collections.Generic;
using ModSettingsMenu.Settings;

namespace RebalanceKeyCrafting
{
    /// <summary>
    /// Mod configuration adapter. The four player-facing knobs — `enabled`,
    /// `reductionFactor`, `minPerIngredient`, `scope` — are now live in-game settings,
    /// read from Mod Settings Menu `SettingHandle`s (bound once in
    /// RebalanceKeyCraftingMod.Init via Bind). The getters stay source-compatible with the
    /// former fields, so the patch (KeyRecipeCostPatch) reads ModConfig.Instance.* unchanged.
    /// The RoslynCSharp sandbox blocks System.IO; the framework persists the values via
    /// CoreLib, so the mod's own code still touches no file API. ObjectID lives in the global
    /// namespace, so it needs no using directive here.
    ///
    /// BAKE-TIME semantics: the recipe rewrite runs once per world in
    /// PugDatabasePostConverter.PostConvert and is idempotent (a static _processed guard). The
    /// getters read the live handle so the NEXT bake picks up the current values, but changing a
    /// setting mid-session does NOT re-rewrite already-baked recipes — it takes effect on the
    /// next game restart. (Hence the section hint says "Changes apply on restart".)
    /// </summary>
    public sealed class ModConfig
    {
        public enum Scope
        {
            /// <summary>Only the seven tier chest keys (Copper..Relucite).</summary>
            TierKeysOnly,
            /// <summary>Any object whose id name ends in "Key" with a non-empty recipe.</summary>
            AllCraftableKeys,
        }

        /// <summary>The discrete crafting-cost choices (cheapest -> off), each mapped to a
        /// reductionFactor. Choice tokens are the enum names, localized per option.</summary>
        public enum Reduction
        {
            /// <summary>Every ingredient reduced to 1 (factor 0, floored to minPerIngredient = 1).</summary>
            OneIngot,
            /// <summary>A quarter of vanilla cost (factor 0.25). Default.</summary>
            Quarter,
            /// <summary>Half of vanilla cost (factor 0.5).</summary>
            Half,
            /// <summary>No reduction (factor 1, vanilla cost).</summary>
            Vanilla,
        }

        private static ModConfig _instance;
        public static ModConfig Instance => _instance ??= new ModConfig();

        // Live handles set once by RebalanceKeyCraftingMod.Init via Bind(); null only in the brief
        // pre-Bind window at mod load -> the hardcoded defaults below apply (Bind runs before the
        // first PostConvert bake, and the framework is a hard dependency, never absent).
        private SettingHandle<bool> _enabledHandle;
        private SettingHandle<Reduction> _reductionHandle;
        private SettingHandle<Scope> _scopeHandle;

        public void Bind(SettingHandle<bool> enabled, SettingHandle<Reduction> reduction,
            SettingHandle<Scope> scope)
        {
            _enabledHandle = enabled;
            _reductionHandle = reduction;
            _scopeHandle = scope;
        }

        /// <summary>Master switch. When false the patch lets vanilla run untouched. Toggle (default on).</summary>
        public bool enabled => _enabledHandle != null ? _enabledHandle.Value : true;

        /// <summary>Each ingredient amount is multiplied by this factor (&lt; 1 reduces). Choice-driven:
        /// OneIngot -> 0 (floored to minPerIngredient = 1 per ingredient), Quarter -> 0.25, Half -> 0.5,
        /// Vanilla -> 1 (no reduction).</summary>
        public float reductionFactor
        {
            get
            {
                switch (_reductionHandle != null ? _reductionHandle.Value : Reduction.Quarter)
                {
                    case Reduction.OneIngot: return 0f;
                    case Reduction.Half:    return 0.5f;
                    case Reduction.Vanilla: return 1f;
                    default:                return 0.25f; // Quarter
                }
            }
        }

        /// <summary>Floor applied after scaling so crafting never becomes free. Hardcoded to 1 (no longer a
        /// menu setting); it is what makes the OneIngot choice resolve to exactly 1 per ingredient (not 0).</summary>
        public int minPerIngredient => 1;

        /// <summary>Which keys are affected. Default ships the tier keys only. Choice.</summary>
        public Scope scope => _scopeHandle != null ? _scopeHandle.Value : Scope.TierKeysOnly;

        /// <summary>The seven tier chest keys (used when scope == TierKeysOnly).</summary>
        public readonly HashSet<ObjectID> tierKeyIds = new HashSet<ObjectID>
        {
            ObjectID.CopperKey,
            ObjectID.IronKey,
            ObjectID.ScarletKey,
            ObjectID.OctarineKey,
            ObjectID.GalaxiteKey,
            ObjectID.SolariteKey,
            ObjectID.ReluciteKey,
        };
    }
}
