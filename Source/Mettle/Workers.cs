using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Mettle
{
    /// <summary>
    /// Someone who has been through it, sitting with someone who currently is.
    ///
    /// Distinct from simply being comforted: what helps here is not kindness but recognition. They
    /// are not telling the other colonist it will be fine. They are the evidence that it can be.
    /// Only a colonist who has actually come through something can do this, which is the whole
    /// point -- it is the part of growth that other people get the benefit of.
    /// </summary>
    public class InteractionWorker_SitWith : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (!Tuning.SitWith || !MT.IsSteadied(initiator) || !MT.Counts(initiator))
            {
                return 0f;
            }
            if (recipient == null || recipient.Dead || recipient.needs?.mood == null)
            {
                return 0f;
            }
            // Only for someone actually struggling. Sitting with the contented is just conversation.
            if (recipient.needs.mood.CurLevelPercentage > Tuning.StrugglingBelow)
            {
                return 0f;
            }
            float w = Tuning.SitWithWeight;
            if (MT.IsWeathered(initiator))
            {
                w *= 1.5f;
            }
            return w;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            if (recipient?.needs?.mood == null)
            {
                return;
            }
            recipient.needs.mood.thoughts.memories.TryGainMemory(MT_DefOf.MT_SatWithMe, initiator);
        }
    }
}
