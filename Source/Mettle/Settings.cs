using UnityEngine;
using Verse;

namespace Mettle
{
    public class MettleSettings : ModSettings
    {
        public int ordealsToSteady = 3;
        public int ordealsToWeather = 7;
        public float sitWithWeight = 0.35f;
        public float strugglingBelow = 0.35f;

        public bool needsSomeone = true;
        public bool sitWith = true;

        public override void ExposeData()
        {
            MettleSettings d = new MettleSettings();
            Scribe_Values.Look(ref ordealsToSteady, "ordealsToSteady", d.ordealsToSteady);
            Scribe_Values.Look(ref ordealsToWeather, "ordealsToWeather", d.ordealsToWeather);
            Scribe_Values.Look(ref sitWithWeight, "sitWithWeight", d.sitWithWeight);
            Scribe_Values.Look(ref strugglingBelow, "strugglingBelow", d.strugglingBelow);
            Scribe_Values.Look(ref needsSomeone, "needsSomeone", true);
            Scribe_Values.Look(ref sitWith, "sitWith", true);
        }

        public void Reset()
        {
            MettleSettings d = new MettleSettings();
            ordealsToSteady = d.ordealsToSteady;
            ordealsToWeather = d.ordealsToWeather;
            sitWithWeight = d.sitWithWeight;
            strugglingBelow = d.strugglingBelow;
            needsSomeone = sitWith = true;
        }
    }

    public class MettleMod : Mod
    {
        public static MettleSettings Settings = new MettleSettings();

        public MettleMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MettleSettings>();
        }

        public override string SettingsCategory() => "Mettle";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            MettleSettings s = Settings;
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            list.Label("How much it takes");
            s.ordealsToSteady = (int)list.SliderLabeled(
                $"Steadied after coming through {s.ordealsToSteady} things", s.ordealsToSteady, 1f, 10f);
            list.Gap(4f);
            s.ordealsToWeather = (int)list.SliderLabeled(
                $"Weathered after coming through {s.ordealsToWeather} things", s.ordealsToWeather, 2f, 20f);
            if (s.ordealsToWeather <= s.ordealsToSteady)
            {
                s.ordealsToWeather = s.ordealsToSteady + 1;
            }

            list.Gap(12f);
            list.Label("Sitting with people");
            s.sitWithWeight = list.SliderLabeled(
                $"How often they sit with someone, frequency {s.sitWithWeight:0.00}", s.sitWithWeight, 0f, 1f);
            list.Gap(4f);
            s.strugglingBelow = list.SliderLabeled(
                $"Counts as struggling below {s.strugglingBelow:P0} mood", s.strugglingBelow, 0.1f, 0.6f);

            list.Gap(12f);
            list.Label("Optional behaviours");
            list.CheckboxLabeled("Nobody grows alone", ref s.needsSomeone,
                "A colonist only comes out of something changed for the better if somebody was in their corner: a partner, family, or simply another colonist who thinks well of them. Switch this off and enduring it alone is enough.");
            list.CheckboxLabeled("Sitting with people", ref s.sitWith,
                "A colonist who has come through something will go and sit with someone who currently is.");

            list.Gap(12f);
            list.Label("Who can grow is not adjustable. A colonist who cannot be reached by what " +
                       "happens to them cannot be changed by it either, and that is the point rather " +
                       "than a difficulty setting.");

            list.Gap(12f);
            if (list.ButtonText("Reset to defaults"))
            {
                s.Reset();
            }

            list.End();
        }
    }
}
