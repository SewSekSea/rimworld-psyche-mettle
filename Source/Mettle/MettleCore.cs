using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Mettle
{
    [DefOf]
    public static class MT_DefOf
    {
        public static TraitDef MT_Weathered;
        public static ThoughtDef MT_CameThrough;
        public static ThoughtDef MT_Changed;
        public static ThoughtDef MT_SatWithMe;
        public static InteractionDef MT_SitWith;

        static MT_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MT_DefOf));
        }
    }

    /// <summary>All the tuning numbers in one place. The ones players can change come from the settings menu.</summary>
    public static class Tuning
    {
        private static MettleSettings S => MettleMod.Settings;

        /// <summary>Ordeals survived before a colonist steadies, and before they are truly weathered.</summary>
        public static int OrdealsToSteady => S.ordealsToSteady;
        public static int OrdealsToWeather => S.ordealsToWeather;

        /// <summary>How often a weathered colonist sits with someone who is struggling.</summary>
        public static float SitWithWeight => S.sitWithWeight;

        /// <summary>Mood at or below which a colonist counts as struggling enough to be sat with.</summary>
        public static float StrugglingBelow => S.strugglingBelow;

        public static bool NeedsSomeone => S.needsSomeone;
        public static bool SitWith => S.sitWith;

        /// <summary>Opinion that counts as somebody in your corner.</summary>
        public const int CaresAbout = 35;

        /// <summary>A spell downed for less than this is a scrape, not an ordeal.</summary>
        public const int DownedTicksForOrdeal = 15000;

        public const int DegreeSteadied = 1;
        public const int DegreeWeathered = 2;
    }

    [StaticConstructorOnStartup]
    public static class MT
    {
        static MT()
        {
            new Harmony("mettle.growth").PatchAll();
            Bridges.Connect();
            Log.Message("[Mettle] Loaded. Counting what people come through." + Bridges.Report());
        }

        public static bool Counts(Pawn p) =>
            p != null && !p.Dead && p.needs?.mood != null && p.IsColonist && !p.IsPrisoner &&
            !p.DevelopmentalStage.Juvenile();

        public static int Degree(Pawn p)
        {
            Trait t = p?.story?.traits?.GetTrait(MT_DefOf.MT_Weathered);
            return t?.Degree ?? 0;
        }

        public static bool IsSteadied(Pawn p) => Degree(p) >= Tuning.DegreeSteadied;
        public static bool IsWeathered(Pawn p) => Degree(p) >= Tuning.DegreeWeathered;

        /// <summary>
        /// Can this colonist grow at all?
        ///
        /// Not everyone can. Coming through something and being more than you were afterwards
        /// requires having been touched by it in the first place, and the cold traits in this set
        /// are defined by not being. A Psychopath does not grow; nothing reaches them. A Sociopath
        /// does not grow for anything outside a circle of two or three. A Narcissist cannot, because
        /// growth means accepting you were not equal to something, and that is the one thing they
        /// will not do.
        ///
        /// This is not a penalty aimed at those mods. It is the same fact from the other side: the
        /// thing they are missing is precisely the thing this mod is about.
        /// </summary>
        public static bool CanGrow(Pawn p)
        {
            if (!Counts(p))
            {
                return false;
            }
            if (p.story.traits.HasTrait(TraitDefOf.Psychopath))
            {
                return false;
            }
            return !Bridges.IsColdHearted(p);
        }

        /// <summary>
        /// Is there anybody in their corner?
        ///
        /// Growth is not something people reliably do alone, and a mod that handed it out for
        /// enduring things by yourself would be saying something false and a bit cruel. Someone
        /// has to have cared: a bonded relation, or simply a colonist who thinks well of them.
        /// </summary>
        public static bool HasSomeone(Pawn p)
        {
            if (!Tuning.NeedsSomeone)
            {
                return true;
            }
            if (p?.Map == null)
            {
                return false;
            }
            foreach (Pawn other in p.Map.mapPawns.FreeColonistsSpawned)
            {
                if (other == p || other.Dead || other.relations == null)
                {
                    continue;
                }
                if (other.relations.OpinionOf(p) >= Tuning.CaresAbout)
                {
                    return true;
                }
            }
            List<DirectPawnRelation> rels = p.relations?.DirectRelations;
            if (rels != null)
            {
                for (int i = 0; i < rels.Count; i++)
                {
                    DirectPawnRelation r = rels[i];
                    if (r?.otherPawn == null || r.otherPawn.Dead)
                    {
                        continue;
                    }
                    if (r.def == PawnRelationDefOf.Spouse || r.def == PawnRelationDefOf.Lover ||
                        r.def == PawnRelationDefOf.Fiance || r.def.familyByBloodRelation)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
