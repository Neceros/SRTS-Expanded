using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace SRTS
{
    internal enum SettingsCategory { Settings, Stats}
    public enum StatName { massCapacity, minPassengers, maxPassengers, flightSpeed, numberBombs, radiusDrop, bombingSpeed }

    public class SRTS_ModSettings : ModSettings
    {
        public Dictionary<string, SRTS_DefProperties> defProperties;

        public bool passengerLimits;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref passengerLimits, "passengerLimits", true);
            Scribe_Collections.Look<string, SRTS_DefProperties>(ref defProperties, "defProperties", LookMode.Value, LookMode.Deep);
        }
    }

    public class SRTSMod : Mod
    {
        public SRTS_ModSettings settings;
        public static SRTSMod mod;

        public SRTSMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<SRTS_ModSettings>();
            mod = this;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            this.CheckDictionaryValid();

            Listing_Standard listing_Standard = new Listing_Standard();
            Rect settingsCategory = new Rect(inRect.width / 2 - (inRect.width / 12), inRect.y, inRect.width / 6, inRect.height);
            Rect groupSRTS = new Rect(settingsCategory.x - settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);

            listing_Standard.Begin(groupSRTS);
            if(currentPage == SRTS.SettingsCategory.Settings)
            {
                listing_Standard.ButtonText(string.Empty);
            }
            else if(currentPage == SRTS.SettingsCategory.Stats)
            {
                if(listing_Standard.ButtonText(currentKey))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (string s in settings.defProperties.Keys)
                    {
                        options.Add(new FloatMenuOption(s, () => currentKey = s, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    if (!options.Any())
                        options.Add(new FloatMenuOption("NoneBrackets".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            listing_Standard.End();

            listing_Standard.Begin(settingsCategory);
            if(listing_Standard.ButtonText(EnumToString(currentPage)))
            {
                FloatMenuOption op1 = new FloatMenuOption("MainSettings".Translate(), () => currentPage = SRTS.SettingsCategory.Settings, MenuOptionPriority.Default, null, null, 0f, null, null);
                FloatMenuOption op2 = new FloatMenuOption("DefSettings".Translate(), () => currentPage = SRTS.SettingsCategory.Stats, MenuOptionPriority.Default, null, null, 0f, null, null);
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { op1, op2 }));
            }
            listing_Standard.End();

            SRTS_DefProperties props = settings.defProperties[currentKey];
            this.ReferenceDefCheck(ref props);

            Rect propsReset = new Rect(settingsCategory.x + settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);
            listing_Standard.Begin(propsReset);
            if(listing_Standard.ButtonText("ResetDefault".Translate(), "ResetDefaultTooltip".Translate()))
            {
                if(currentPage == SRTS.SettingsCategory.Settings)
                {
                    this.ResetMainSettings();
                }
                else if(currentPage == SRTS.SettingsCategory.Stats)
                {
                    FloatMenuOption op1 = new FloatMenuOption("ResetThisSRTS".Translate(), () => this.ResetToDefaultValues(ref props), MenuOptionPriority.Default, null, null, 0f, null, null);
                    FloatMenuOption op2 = new FloatMenuOption("ResetAll".Translate(), delegate ()
                    {
                        for (int i = 0; i < settings.defProperties.Count; i++)
                        {
                            SRTS_DefProperties p = settings.defProperties.ElementAt(i).Value;
                            this.ReferenceDefCheck(ref p);
                            this.ResetToDefaultValues(ref p);
                        }
                    }, MenuOptionPriority.Default, null, null, 0f, null, null);
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { op1, op2 }));
                }
            }
            listing_Standard.End();

            Rect viewRect = new Rect(0f, -45f, inRect.width, inRect.height);
            Rect group2 = new Rect(inRect.x, inRect.y - 45f, inRect.width / 3, inRect.height);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, true);
            if(currentPage == SRTS.SettingsCategory.Stats)
            {
                listing_Standard.Begin(group2);

                listing_Standard.Settings_Header("SRTSSettings".Translate(), new Color(0f, 0f, 0f, 0.25f));

                listing_Standard.Settings_SliderLabeled("FlightSpeed".Translate(), string.Empty, ref props.flightSpeed, 1, 50, 2);
                if(settings.passengerLimits)
                {
                    listing_Standard.Settings_SliderLabeled("MinPassengers".Translate(), string.Empty, ref props.minPassengers, 1, 100);
                    listing_Standard.Settings_SliderLabeled("MaxPassengers".Translate(), string.Empty, ref props.maxPassengers, 1, 100, 999);
                }
                else
                {
                    listing_Standard.Gap(54f);
                }

                int mass = (int)props.massCapacity;
                listing_Standard.Settings_IntegerBox("CargoCapacity".Translate(), ref mass, 100f, 50f, 0, int.MaxValue);
                props.massCapacity = (float)mass;

                if (props.BombCapable)
                {
                    listing_Standard.Gap(24f);
                    listing_Standard.Settings_Header("BombSettings".Translate(), new Color(0f, 0f, 0f, 0.25f));

                    listing_Standard.Settings_SliderLabeled("BombSpeed".Translate(), string.Empty, ref props.bombingSpeed, 0.5f, 2.5f, 10f, 1);
                    listing_Standard.Settings_SliderLabeled("RadiusDrop".Translate(), " Cells", ref props.radiusDrop, 1, 10);
                    listing_Standard.Settings_IntegerBox("NumberBombs".Translate(), ref props.numberBombs, 100f, 50f, 1, 100);
                }
                listing_Standard.End();

                GraphicRequest graphicRequest = new GraphicRequest(props.referencedDef.graphicData.graphicClass, props.referencedDef.graphicData.texPath, ShaderTypeDefOf.Cutout.Shader, props.referencedDef.graphic.drawSize,
                    Color.white, Color.white, props.referencedDef.graphicData, 0, null);
                string texPath = props.referencedDef.graphicData.texPath;
                if(graphicRequest.graphicClass == typeof(Graphic_Multi))
                    texPath += "_north";
                GUI.DrawTexture(new Rect(inRect.width / 2, inRect.y, 300f, 300f), ContentFinder<Texture2D>.Get(texPath, true));
            }
            if(currentPage == SRTS.SettingsCategory.Settings)
            {
                listing_Standard.Begin(group2);

                listing_Standard.CheckboxLabeled("PassengerLimit".Translate(), ref settings.passengerLimits, "PassengerLimitTooltip".Translate());

                listing_Standard.End();
            }
            Widgets.EndScrollView();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "SRTSExpanded".Translate();
        }

        private void ResetToDefaultValues(ref SRTS_DefProperties props)
        {
            props.massCapacity = props.referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
            props.minPassengers = props.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers;
            props.maxPassengers = props.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers;
            props.flightSpeed = props.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed;
            if(props.BombCapable)
            {
                props.bombingSpeed = props.referencedDef.GetCompProperties<CompProperties_BombsAway>().speed;
                props.numberBombs = props.referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs;
                props.radiusDrop = props.referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop;
            }
        }

        private void ResetMainSettings()
        {
            SoundDefOf.RadioButtonClicked.PlayOneShotOnCamera();
            this.settings.passengerLimits = true;
        }

        public void ReferenceDefCheck(ref SRTS_DefProperties props)
        {
            if(props.referencedDef is null)
            {
                SRTS_DefProperties propsRef = props;
                props.referencedDef = DefDatabase<ThingDef>.GetNamed(settings.defProperties.FirstOrDefault(x => x.Value == propsRef).Key);
            }
        }

        public void CheckDictionaryValid()
        {
            if (settings.defProperties is null || settings.defProperties.Count <= 0)
            {
                settings.defProperties = new Dictionary<string, SRTS_DefProperties>();
                foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.GetCompProperties<CompProperties_LaunchableSRTS>() != null))
                {
                    settings.defProperties.Add(t.defName, new SRTS_DefProperties(t));
                }
            }
            if (currentKey is null || currentKey.NullOrEmpty())
                currentKey = settings.defProperties.Keys.First();
        }

        private string EnumToString(SettingsCategory category)
        {
            switch(category)
            {
                case SRTS.SettingsCategory.Settings:
                    return "MainSettings".Translate();
                case SRTS.SettingsCategory.Stats:
                    return "DefSettings".Translate();
            }
            Log.Warning("Setting Category " + category.ToString() + " not yet implemented. - Smash Phil");
            return category.ToString();
        }

        public static T GetStatFor<T>(string defName, StatName stat)
        {
            switch(stat)
            {
                case StatName.massCapacity:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].massCapacity, typeof(T));
                case StatName.minPassengers:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].minPassengers, typeof(T));
                case StatName.maxPassengers:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].maxPassengers, typeof(T));
                case StatName.flightSpeed:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].flightSpeed, typeof(T));
                case StatName.bombingSpeed:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].bombingSpeed, typeof(T));
                case StatName.numberBombs:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].numberBombs, typeof(T));
                case StatName.radiusDrop:
                    return (T)Convert.ChangeType(SRTSMod.mod.settings.defProperties[defName].radiusDrop, typeof(T));
            }
            return default;
        }

        public string currentKey;

        private SettingsCategory currentPage;

        public Vector2 scrollPosition;
    }

    public class SRTS_DefProperties : IExposable
    {
        public SRTS_DefProperties()
        {
        }

        public SRTS_DefProperties(ThingDef def)
        {
            referencedDef = def;
            this.massCapacity = referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
            this.minPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers;
            this.maxPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers;
            this.flightSpeed = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed;
            if(BombCapable)
            {
                this.numberBombs = referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs;
                this.radiusDrop = referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop;
                this.bombingSpeed = referencedDef.GetCompProperties<CompProperties_BombsAway>().speed;
            }
        }

        /// <summary>
        /// Base SRTS values
        /// </summary>
        public ThingDef referencedDef;

        public float massCapacity = 0;

        public int minPassengers = 1;

        public int maxPassengers = 2;

        public float flightSpeed = 10;

        /// <summary>
        /// Bomb related values
        /// </summary>
        public bool BombCapable
        {
            get
            {
                if(referencedDef is null)
                    referencedDef = DefDatabase<ThingDef>.GetNamed(SRTSMod.mod.settings.defProperties.FirstOrDefault(x => x.Value == this).Key);
                return referencedDef.GetCompProperties<CompProperties_BombsAway>() != null;
            }
        }
        public int numberBombs = 0;

        public int radiusDrop = 1;

        public float bombingSpeed = 1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref massCapacity, "massCapacity");
            Scribe_Values.Look(ref minPassengers, "minPassengers");
            Scribe_Values.Look(ref maxPassengers, "maxPassengers");
            Scribe_Values.Look(ref flightSpeed, "flightSpeed");

            Scribe_Values.Look(ref numberBombs, "numberBombs");
            Scribe_Values.Look(ref radiusDrop, "radiusDrop");
            Scribe_Values.Look(ref bombingSpeed, "bombingSpeed");
        }
    }
}
