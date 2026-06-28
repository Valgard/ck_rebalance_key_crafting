using System.Collections.Generic;

namespace RebalanceKeyCrafting
{
    /// <summary>
    /// Hardcoded configuration singleton. The RoslynCSharp sandbox blocks
    /// System.IO, so there is no runtime config file; the singleton shape is
    /// preserved so a future sandbox-safe loader (API.ConfigFilesystem) could
    /// drop in without touching the patch. ObjectID lives in the global
    /// namespace, so it needs no using directive here.
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

        private static ModConfig _instance;
        public static ModConfig Instance => _instance ??= new ModConfig();

        /// <summary>Master switch. When false the patch lets vanilla run untouched.</summary>
        public readonly bool enabled = true;

        /// <summary>Each ingredient amount is multiplied by this factor (&lt; 1 reduces).</summary>
        public readonly float reductionFactor = 0.25f;

        /// <summary>Floor applied after scaling so crafting never becomes free.</summary>
        public readonly int minPerIngredient = 1;

        /// <summary>Which keys are affected. Default ships the tier keys only.</summary>
        public readonly Scope scope = Scope.TierKeysOnly;

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
