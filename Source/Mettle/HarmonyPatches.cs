using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Mettle
{
    /// <summary>
    /// Coming out of a breakdown.
    ///
    /// This is the clearest ordeal the base game has: a colonist lost hold of themselves and then
    /// got it back. Only real breaks count -- the brief mood-driven wobbles are excluded, because
    /// a colonist who sulks for an hour has not come through anything.
    /// </summary>
    [HarmonyPatch(typeof(MentalState), nameof(MentalState.RecoverFromState))]
    public static class Patch_RecoverFromState
    {
        public static void Postfix(MentalState __instance)
        {
            Pawn pawn = __instance?.pawn;
            if (pawn == null || __instance.def == null || !MT.Counts(pawn))
            {
                return;
            }
            // Wandering sadly for a few hours is not an ordeal. Something that had to be dealt with is.
            if (__instance.def.recoveryMtbDays > 0f && __instance.def.recoveryMtbDays < 0.5f)
            {
                return;
            }
            GameComponent_Mettle.Instance?.NoteOrdeal(pawn, "recovered from " + __instance.def.defName);
        }
    }

    /// <summary>
    /// Keep the trait out of character generation.
    ///
    /// This has to be done in code. The obvious approach -- a commonality of zero on every degree --
    /// looks right and is a trap: the generator picks a degree by weighted random, an all-zero
    /// weighting returns nothing, and RimWorld then dereferences it and throws. That crash lands in
    /// PawnGenerator during ordinary pawn creation, so it would have broken new colonists, raids and
    /// visitors alike for anyone running the mod.
    ///
    /// Forcing the selection weight to zero instead leaves the degree numbers intact for the code
    /// paths that expect them, while making the trait unreachable by the generator.
    /// </summary>
    [HarmonyPatch(typeof(TraitDef), nameof(TraitDef.GetGenderSpecificCommonality))]
    public static class Patch_GetGenderSpecificCommonality
    {
        public static void Postfix(TraitDef __instance, ref float __result)
        {
            if (__instance == MT_DefOf.MT_Weathered)
            {
                __result = 0f;
            }
        }
    }

    /// <summary>
    /// Belt and braces: if the trait ever reaches a generated pawn by some route the above does not
    /// cover, take it straight back off again. It is only ever meant to be earned in play.
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraitsFor")]
    public static class Patch_GenerateTraitsFor
    {
        public static void Postfix(List<Trait> __result)
        {
            __result?.RemoveAll(t => t?.def == MT_DefOf.MT_Weathered);
        }
    }

    /// <summary>
    /// A weathered colonist is harder to knock over, but the trait only goes so far. This stops the
    /// mood floor from turning into immunity: the effect is capped rather than stacking with
    /// everything else a colonist might have.
    /// </summary>
    [HarmonyPatch(typeof(MentalBreaker), nameof(MentalBreaker.TryDoRandomMoodCausedMentalBreak))]
    public static class Patch_TryDoRandomMoodCausedMentalBreak
    {
        public static bool Prefix(MentalBreaker __instance, ref bool __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!MT.IsWeathered(pawn))
            {
                return true;
            }
            // One in four breaks simply does not land on someone who has been here before. They
            // still break -- this is a resistance, not a shield.
            if (!Rand.Chance(0.25f))
            {
                return true;
            }
            __result = false;
            return false;
        }
    }
}
