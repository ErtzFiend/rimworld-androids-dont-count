using System;
using System.Collections.Generic;
using Verse;

namespace AndroidsDontCount
{
    /// <summary>
    /// Exact classifier for android pawns based on Androids mod conventions:
    /// 1. DefModExtension Androids.MechanicalPawnProperties on pawn.def
    /// 2. Hediff defName ChjAndroidLike on pawn.health.hediffSet
    /// </summary>
    public static class AndroidClassifier
    {
        public const string MechanicalPropertiesType = "Androids.MechanicalPawnProperties";
        public const string AndroidLikeHediffDefName = "ChjAndroidLike";

        public static bool IsAndroid(Pawn pawn)
        {
            if (pawn == null || pawn.def == null) return false;

            var extensions = pawn.def.modExtensions;
            if (extensions != null)
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    var ext = extensions[i];
                    if (ext == null) continue;
                    if (string.Equals(ext.GetType().FullName, MechanicalPropertiesType, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            var hediffSet = pawn.health?.hediffSet;
            if (hediffSet?.hediffs != null)
            {
                var hediffs = hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    var h = hediffs[i];
                    if (h?.def != null && string.Equals(h.def.defName, AndroidLikeHediffDefName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsAndroidCorpse(Corpse corpse)
        {
            if (corpse == null) return false;
            try
            {
                return IsAndroid(corpse.InnerPawn);
            }
            catch
            {
                return false;
            }
        }
    }
}
