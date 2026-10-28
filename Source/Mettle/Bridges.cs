using HarmonyLib;
using RimWorld;
using Verse;

namespace Mettle
{
    /// <summary>
    /// Soft links to the rest of the set. Everything here is looked up by name at startup and
    /// simply does nothing if the other mod is absent. Nothing in this file is required.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Bridges
    {
        private static TraitDef sociopath;
        private static TraitDef narcissist;
        private static bool solacePresent;
        private static bool griefPresent;

        public static void Connect()
        {
            sociopath = DefDatabase<TraitDef>.GetNamedSilentFail("SP_Sociopath");
            narcissist = DefDatabase<TraitDef>.GetNamedSilentFail("NR_Narcissist");
            try
            {
                solacePresent = AccessTools.TypeByName("Solace.SL") != null;
                griefPresent = AccessTools.TypeByName("Grief.GameComponent_Grief") != null;
            }
            catch
            {
                solacePresent = false;
                griefPresent = false;
            }
        }

        public static string Report()
        {
            string s = "";
            if (sociopath != null) { s += " Sociopath found: the cold do not grow."; }
            if (narcissist != null) { s += " Narcissist found: nor do they."; }
            if (solacePresent) { s += " Solace found: being sat with is company on the way through."; }
            if (griefPresent) { s += " Grief found."; }
            return s;
        }

        /// <summary>
        /// A colonist who cannot be reached by what happens to them, and therefore cannot be
        /// changed by it. Only the full degree of each trait: the milder forms still feel things.
        /// </summary>
        public static bool IsColdHearted(Pawn p)
        {
            if (p?.story?.traits == null)
            {
                return false;
            }
            if (sociopath != null)
            {
                Trait t = p.story.traits.GetTrait(sociopath);
                if (t != null && t.Degree >= 2) { return true; }
            }
            if (narcissist != null)
            {
                Trait t = p.story.traits.GetTrait(narcissist);
                if (t != null && t.Degree >= 2) { return true; }
            }
            return false;
        }
    }
}
