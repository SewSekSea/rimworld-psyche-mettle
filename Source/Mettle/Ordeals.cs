using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Mettle
{
    /// <summary>
    /// The ledger of what people have come through.
    ///
    /// Deliberately counts survivals, not sufferings. An ordeal only registers when it is over and
    /// the colonist is still standing: a mental break they recovered from, a wound they were carried
    /// away from and got up again after. Something they are still in the middle of counts for
    /// nothing, because they have not come through it yet.
    /// </summary>
    public class GameComponent_Mettle : GameComponent
    {
        private const int TickInterval = 500;

        private Dictionary<int, int> ordeals = new Dictionary<int, int>();
        private List<int> ordealKeys;
        private List<int> ordealValues;

        /// <summary>Who is currently down, and since when, so we can tell a scrape from an ordeal.</summary>
        private readonly Dictionary<int, int> downedSince = new Dictionary<int, int>();

        private static GameComponent_Mettle instance;

        public GameComponent_Mettle(Game game)
        {
            instance = this;
        }

        public static GameComponent_Mettle Instance =>
            instance ?? Current.Game?.GetComponent<GameComponent_Mettle>();

        public override void FinalizeInit() => instance = this;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref ordeals, "ordeals", LookMode.Value, LookMode.Value,
                ref ordealKeys, ref ordealValues);
            if (ordeals == null)
            {
                ordeals = new Dictionary<int, int>();
            }
        }

        public int OrdealsFor(Pawn p) =>
            p != null && ordeals.TryGetValue(p.thingIDNumber, out int n) ? n : 0;

        /// <summary>Exposed so tests can set up a colonist who has been through things.</summary>
        public void SetOrdeals(Pawn p, int n)
        {
            if (p != null)
            {
                ordeals[p.thingIDNumber] = n;
            }
        }

        /// <summary>
        /// Something hard is over and they are still here.
        ///
        /// Counted for everyone, including colonists who can never grow -- the ledger is a record of
        /// what happened, not a scoreboard of who deserves something. Only the growing is gated.
        /// </summary>
        public void NoteOrdeal(Pawn p, string what)
        {
            if (!MT.Counts(p))
            {
                return;
            }
            int now = OrdealsFor(p) + 1;
            ordeals[p.thingIDNumber] = now;
            p.needs.mood.thoughts.memories.TryGainMemory(MT_DefOf.MT_CameThrough);
            TryGrow(p, what);
        }

        /// <summary>
        /// Has this tipped them over into being someone different?
        ///
        /// Both thresholds can be crossed by the same ordeal if the ledger jumped, so this checks
        /// the higher one first and only ever moves a colonist forwards.
        /// </summary>
        private void TryGrow(Pawn p, string what)
        {
            if (!MT.CanGrow(p))
            {
                return;
            }
            int count = OrdealsFor(p);
            int has = MT.Degree(p);
            int earned = count >= Tuning.OrdealsToWeather ? Tuning.DegreeWeathered
                       : count >= Tuning.OrdealsToSteady ? Tuning.DegreeSteadied
                       : 0;
            if (earned <= has)
            {
                return;
            }
            if (!MT.HasSomeone(p))
            {
                // They endured it alone. Nothing is taken away from them -- they simply do not
                // come out of it changed for the better, and the ledger keeps counting.
                return;
            }

            TraitSet traits = p.story.traits;
            Trait existing = traits.GetTrait(MT_DefOf.MT_Weathered);
            if (existing != null)
            {
                traits.RemoveTrait(existing);
            }
            traits.GainTrait(new Trait(MT_DefOf.MT_Weathered, earned));
            p.needs.AddOrRemoveNeedsAsAppropriate();
            p.needs.mood.thoughts.memories.TryGainMemory(MT_DefOf.MT_Changed);

            string label = earned >= Tuning.DegreeWeathered ? "Weathered" : "Steadied";
            Find.LetterStack.ReceiveLetter(
                $"{label}: {p.LabelShortCap}",
                earned >= Tuning.DegreeWeathered
                    ? $"{p.LabelShortCap} has come through more than most people are asked to, and is still here, and is still themselves. Very little rattles them now. The others have started going to them when it is bad."
                    : $"{p.LabelShortCap} has been through something and come out the other side of it. They are harder to knock over than they were, and better than most at sitting with someone who is struggling.",
                LetterDefOf.PositiveEvent, p);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % TickInterval != 0)
            {
                return;
            }
            WatchForRecovery();
        }

        /// <summary>
        /// Being carried off the field and getting up again.
        ///
        /// Watched here rather than patched, because there is no single moment in the game that
        /// means "recovered" -- only a pawn who was down and now is not. A short spell does not
        /// count; being taken out of a fight for a moment is not something you come through.
        /// </summary>
        private void WatchForRecovery()
        {
            int now = Find.TickManager.TicksGame;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                // Snapshot, do not iterate the live list.
                //
                // FreeColonistsSpawned hands out ONE cached List<Pawn> per faction and calls
                // Clear() on it at the start of every single access (MapPawns.FreeHumanlikesSpawnedOfFaction).
                // So any nested read of it — here, NoteOrdeal -> TryGrow -> MT.HasSomeone, which
                // loops the colony looking for somebody who cares — empties the very list this
                // foreach is walking, and the next MoveNext() throws "Collection was modified".
                //
                // It only fired on the tick a colonist crossed an ordeal threshold by recovering
                // from being downed, which is why it shipped.
                List<Pawn> colonists = new List<Pawn>(maps[m].mapPawns.FreeColonistsSpawned);
                for (int c = 0; c < colonists.Count; c++)
                {
                    Pawn p = colonists[c];
                    if (!MT.Counts(p))
                    {
                        continue;
                    }
                    int id = p.thingIDNumber;
                    bool down = p.Downed;
                    bool wasDown = downedSince.TryGetValue(id, out int since);

                    if (down && !wasDown)
                    {
                        downedSince[id] = now;
                    }
                    else if (!down && wasDown)
                    {
                        downedSince.Remove(id);
                        if (now - since >= Tuning.DownedTicksForOrdeal)
                        {
                            NoteOrdeal(p, "recovered");
                        }
                    }
                }
            }
        }
    }
}
