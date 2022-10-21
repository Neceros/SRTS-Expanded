using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using SPExtended;

namespace SRTS
{
    internal enum SettingsCategory { Settings, Stats, Research }
    public enum StatName { massCapacity, minPassengers, maxPassengers, flightSpeed, numberBombs, radiusDrop, bombingSpeed, distanceBetweenDrops, researchPoints, fuelPerTile, precisionBombingNumBombs
        , spaceFaring, shuttleBayLanding}

    public class SRTS_ModSettings : ModSettings
    {
        public Dictionary<string, SRTS_DefProperties> defProperties = new Dictionary<string, SRTS_DefProperties>();

        public bool passengerLimits = true;
        public bool displayHomeItems = true;
        public bool disableAdvancedRecipes = false;
        public bool expandBombPoints = true;
        public bool dynamicWorldDrawingSRTS = true;

        public bool allowEvenIfDowned = true;
        public bool allowEvenIfPrisonerUnsecured = false;
        public bool allowCapturablePawns = true;

        public float buildCostMultiplier = 1f;
        public List<string> allowedBombs = new List<string>();
        public List<string> disallowedBombs = new List<string>();

        internal bool CEPreviouslyInitialized;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref passengerLimits, "passengerLimits", true);
            Scribe_Values.Look(ref displayHomeItems, "displayHomeItems", true);
            Scribe_Values.Look(ref expandBombPoints, "expandBombPoints", true);
            Scribe_Values.Look(ref dynamicWorldDrawingSRTS, "dynamicWorldDrawingSRTS", true);

            Scribe_Values.Look(ref allowEvenIfDowned, "allowEvenIfDowned", true);
            Scribe_Values.Look(ref allowEvenIfPrisonerUnsecured, "allowEvenIfPrisonerUnsecured", false);
            Scribe_Values.Look(ref allowCapturablePawns, "allowCapturablePawns", true);

            Scribe_Collections.Look<string, SRTS_DefProperties>(ref defProperties, "defProperties", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look<string>(ref allowedBombs, "allowedBombs", LookMode.Value);
            Scribe_Collections.Look<string>(ref disallowedBombs, "disallowedBombs", LookMode.Value);

            Scribe_Values.Look(ref CEPreviouslyInitialized, "CEPreviouslyInitialized");
        }

        public void CheckDictionarySavedValid()
        {
            if (this.defProperties is null || this.defProperties.Count <= 0)
            {
                this.defProperties = new Dictionary<string, SRTS_DefProperties>();
                
                foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.GetCompProperties<CompProperties_LaunchableSRTS>() != null))
                {
                    this.defProperties.Add(t.defName, new SRTS_DefProperties(t));
                }
            }
        }

        public static void CheckNewDefaultValues()
        {
            SRTSMod.mod.settings.CheckDictionarySavedValid();
            foreach (KeyValuePair<string, SRTS_DefProperties> kvp in SRTSMod.mod.settings.defProperties)
            {
                if(SRTSMod.mod.settings.defProperties.ContainsKey(kvp.Key))
                {
                    if (SRTSMod.mod.settings.defProperties[kvp.Key].ResetReferencedDef(kvp.Key))
                    {
                        if (SRTSMod.mod.settings.defProperties[kvp.Key].defaultValues)
                        {
                            SRTSMod.mod.settings.defProperties[kvp.Key].ResetToDefaultValues();
                        }
                        continue;
                    }
                }
                Log.Warning("[SRTSExpanded] Unable to perform loading procedures on key (" + kvp.Key + "). Performing a hard reset on the ModSettings.");
                SRTSMod.mod.settings.defProperties.Clear();
                SRTSMod.mod.CheckDictionaryValid();
                break;
            }
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

            var font = Text.Font;
            Text.Font = GameFont.Tiny;
            string credit = "Settings by Smash Phil";
            //Widgets.Label(new Rect(inRect.width - (6 * credit.Count()), inRect.height + 64f, inRect.width, inRect.height), credit);
            Text.Font = font;

            Listing_Standard listing_Standard = new Listing_Standard();
            Rect settingsCategory = new Rect(inRect.width / 2 - (inRect.width / 12), inRect.y, inRect.width / 6, inRect.height);
            Rect groupSRTS = new Rect(settingsCategory.x - settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);

            if(Prefs.DevMode)
            {
                Rect emergencyReset = new Rect(inRect.width - settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);
                listing_Standard.Begin(emergencyReset);
                if(listing_Standard.ButtonText("DevMode Reset"))
                {
                    this.settings.defProperties.Clear();
                    this.CheckDictionaryValid();
                    this.ResetMainSettings();
                    Log.Message("========================== \n DevMode Settings Reset:");
                    foreach(KeyValuePair<string, SRTS_DefProperties> pair in settings.defProperties)
                    {
                        Log.Message("KVP: " + pair.Key + " : " + pair.Value.referencedDef.defName);
                    }
                    Log.Message("==========================");
                }
                listing_Standard.End();
            }

            listing_Standard.Begin(groupSRTS);
            if(currentPage == SRTS.SettingsCategory.Settings)
            {
                listing_Standard.ButtonText(string.Empty);
            }
            else if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
            {
                if (listing_Standard.ButtonText(currentKey))
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
                FloatMenuOption op3 = new FloatMenuOption("ResearchSettings".Translate(), () => currentPage = SRTS.SettingsCategory.Research, MenuOptionPriority.Default, null, null, 0f, null, null);
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { op1, op2 })); //Removed SettingsCategory.Research from float menu, removing all research related patches. DO NOT ADD BACK
            }
            listing_Standard.End();

            props = settings.defProperties[currentKey];
            if(props is null)
            {
                this.ResetMainSettings();
                settings.defProperties.Clear();
                this.CheckDictionaryValid();
                props = settings.defProperties[currentKey];
            }
            this.ReferenceDefCheck(ref props);

            Rect propsReset = new Rect(settingsCategory.x + settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);
            listing_Standard.Begin(propsReset);
            if (listing_Standard.ButtonText("ResetDefault".Translate(), "ResetDefaultTooltip".Translate()))
            {
                if (currentPage == SRTS.SettingsCategory.Settings)
                {
                    this.ResetMainSettings();
                }
                else if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
                {
                    FloatMenuOption op1 = new FloatMenuOption("ResetThisSRTS".Translate(), () => props.ResetToDefaultValues(), MenuOptionPriority.Default, null, null, 0f, null, null);
                    FloatMenuOption op2 = new FloatMenuOption("ResetAll".Translate(), delegate ()
                    {
                        for (int i = 0; i < settings.defProperties.Count; i++)
                        {
                            SRTS_DefProperties p = settings.defProperties.ElementAt(i).Value;
                            this.ReferenceDefCheck(ref p);
                            p.ResetToDefaultValues();
                        }
                    }, MenuOptionPriority.Default, null, null, 0f, null, null);
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { op1, op2 }));
                }
            }
            listing_Standard.End();

            Rect group2 = new Rect(inRect.x, settingsCategory.y + 36f, inRect.width / 3, inRect.height);

            if (currentPage == SRTS.SettingsCategory.Stats)
            {
                listing_Standard.Begin(group2);

                listing_Standard.Settings_Header("SRTSSettings".Translate(), DialogSettings.highlightColor);

                listing_Standard.Settings_SliderLabeled("FlightSpeed".Translate(), string.Empty, ref props.flightSpeed, 0.15f, 50, 2, 2, 9999f, "Instant".Translate());

                listing_Standard.Settings_SliderLabeled("FuelEfficiency".Translate(), "FuelEfficiencySymbol".Translate(), ref props.fuelPerTile, 1, 6f);
                
                if(settings.passengerLimits)
                {
                    int min = props.minPassengers;
                    int max = props.maxPassengers;
                    listing_Standard.Settings_SliderLabeled("MinPassengers".Translate(), string.Empty, ref props.minPassengers, 1, 100);
                    listing_Standard.Settings_SliderLabeled("MaxPassengers".Translate(), string.Empty, ref props.maxPassengers, 1, 100, 999, "\u221E");
                    if(props.minPassengers > props.maxPassengers && min != props.minPassengers)
                        props.maxPassengers = props.minPassengers;
                    if(props.maxPassengers < props.minPassengers && max != props.maxPassengers)
                        props.minPassengers = props.maxPassengers;
                }
                else
                {
                    listing_Standard.Gap(54f);
                }

                int mass = (int)props.massCapacity;
                listing_Standard.Settings_IntegerBox("CargoCapacity".Translate(), ref mass, 100f, 50f, 0, int.MaxValue);
                props.massCapacity = (float)mass;

                listing_Standard.Gap(12f);

                if (props.BombCapable)
                {
                    listing_Standard.Gap(24f);
                    listing_Standard.Settings_Header("BombSettings".Translate(), DialogSettings.highlightColor);

                    listing_Standard.Settings_SliderLabeled("BombSpeed".Translate(), string.Empty, ref props.bombingSpeed, 0.5f, 2.5f, 10f, 1);
                    listing_Standard.Settings_SliderLabeled("RadiusDrop".Translate(), "CellsEndValue".Translate(), ref props.radiusDrop, 1, 10);
                    listing_Standard.Settings_SliderLabeled("DistanceBetweenDrops".Translate(), "CellsEndValue".Translate(), ref props.distanceBetweenDrops, 0.2f, 11, 1, 1, 999, "SingleDrop".Translate());

                    listing_Standard.Settings_Header("BombCountSRTS".Translate(), DialogSettings.highlightColor, GameFont.Tiny, TextAnchor.MiddleLeft);
                    listing_Standard.Settings_SliderLabeled("PreciseBombing".Translate(), string.Empty, ref props.precisionBombingNumBombs, 1, 10);
                    listing_Standard.Settings_SliderLabeled("CarpetBombing".Translate(), string.Empty, ref props.numberBombs, 1, 40);
                }
                listing_Standard.End();

                if(SRTSHelper.SOS2ModLoaded)
                {
                    Rect sos2Rect = new Rect(inRect.width - (inRect.width / 4), inRect.height - (inRect.height / 8), inRect.width / 4, inRect.height / 4);

                    listing_Standard.Begin(sos2Rect);

                    listing_Standard.Settings_Header("SOS2Compatibility".Translate(), DialogSettings.highlightColor, GameFont.Small, TextAnchor.MiddleLeft);
                    listing_Standard.CheckboxLabeled("SpaceFaring".Translate(), ref props.spaceFaring);
                    listing_Standard.CheckboxLabeled("ShuttleBayLanding".Translate(), ref props.shuttleBayLanding);

                    listing_Standard.End();
                }
            }
            else if (currentPage == SRTS.SettingsCategory.Research)
            {
                listing_Standard.Begin(group2);

                listing_Standard.Settings_Header("ResearchDef".Translate(props.RequiredResearch[0].LabelCap), DialogSettings.highlightColor);

                int rPoints = (int)props.researchPoints;
                //listing_Standard.Settings_IntegerBox("SRTSResearch".Translate(), ref rPoints, 100f, 50f, 0, int.MaxValue);
                props.researchPoints = (float)rPoints;

                listing_Standard.Gap(24f);

                listing_Standard.Settings_Header("SRTSResearchRequirements".Translate(), DialogSettings.highlightColor, GameFont.Small);
                
                foreach(ResearchProjectDef proj in props.requiredResearch)
                {
                    listing_Standard.Settings_Header(proj.LabelCap, Color.clear, GameFont.Small);
                }
                for(int i = props.CustomResearch.Count - 1; i >= 0; i--)
                {
                    ResearchProjectDef proj = props.customResearchRequirements[i];
                    if(listing_Standard.Settings_ButtonLabeled(proj.LabelCap, "RemoveResearch".Translate(), Color.cyan, 60f, false, true))
                    {
                        props.RemoveCustomResearch(proj);
                    }
                    listing_Standard.Gap(8f);
                }

                if(listing_Standard.Settings_Button("AddItemSRTS".Translate(), new Rect(group2.width - 60f, group2.y + 24f, 60f, 20f), Color.white, true, true))
                {
                    Find.WindowStack.Add(new Dialog_ResearchChange());
                }

                listing_Standard.Gap(24f);

                listing_Standard.End();
            }
            else if(currentPage == SRTS.SettingsCategory.Settings)
            {
                if(!checkValidityBombs)
                {
                    ///<summary>Only run once, ensure that removed ThingDefs do not show defNames inside inner list.</summary>
                    for(int i = mod.settings.allowedBombs.Count - 1; i >= 0; i--)
                    {
                        string s = mod.settings.allowedBombs[i];
                        if (mod.settings.allowedBombs.Contains(s) && DefDatabase<ThingDef>.GetNamedSilentFail(s) is null)
                            mod.settings.allowedBombs.Remove(s);
                        if (mod.settings.disallowedBombs.Contains(s) && DefDatabase<ThingDef>.GetNamedSilentFail(s) is null)
                            mod.settings.disallowedBombs.Remove(s);
                    }
                    checkValidityBombs = true;
                }

                listing_Standard.Begin(group2);

                listing_Standard.CheckboxLabeled("PassengerLimit".Translate(), ref settings.passengerLimits, "PassengerLimitTooltip".Translate());
                listing_Standard.CheckboxLabeled("DisplayHomeItems".Translate(), ref settings.displayHomeItems, "DisplayHomeItemsTooltip".Translate());
                listing_Standard.CheckboxLabeled("DynamicWorldObjectSRTS".Translate(), ref settings.dynamicWorldDrawingSRTS, "DynamicWorldObjectSRTSTooltip".Translate());

                listing_Standard.Gap(24f);

                listing_Standard.CheckboxLabeled("ExpandBombPoints".Translate(), ref settings.expandBombPoints, "ExpandBombPointsTooltip".Translate());

                listing_Standard.End();

                int numBoxesBefore = 4;
                float bufferFromGroup2 = 24f + (24 * numBoxesBefore);
                Rect bombLabel = new Rect(group2.x, group2.y + bufferFromGroup2, group2.width, group2.height / 3);
                listing_Standard.Begin(bombLabel);

                listing_Standard.Settings_Header("AllowedBombs".Translate(), DialogSettings.highlightColor, GameFont.Small);
                float buttonWidth = 60f;
                if(listing_Standard.Settings_Button("ChangeItemSRTS".Translate(), new Rect(group2.width - buttonWidth, 1f, buttonWidth, 20f), Color.white, true, true))
                {
                    Find.WindowStack.Add(new Dialog_AllowedBombs());
                }
                listing_Standard.End();
                Rect group3 = new Rect(group2.x, bombLabel.y + 24f, group2.width, group2.height / 3);
                Rect viewRect = new Rect(group3.x, group3.y, group2.width - 24f, group3.height * ((float)mod.settings.allowedBombs.Count / 6f) + 24f);

                Widgets.BeginScrollView(group3, ref scrollPosition, viewRect, true);
                listing_Standard.Begin(viewRect);

                foreach(string s in mod.settings.allowedBombs)
                {
                    listing_Standard.Settings_Header(s, Color.clear, GameFont.Small);
                }

                listing_Standard.End();
                Widgets.EndScrollView();

                Rect transportGroupRect = new Rect(inRect.width - inRect.width / 3, group2.y, inRect.width / 3, group2.height);

                listing_Standard.Begin(transportGroupRect);

                listing_Standard.Settings_Header("SRTSBoardingOptions".Translate(), DialogSettings.highlightColor, GameFont.Small);

                listing_Standard.CheckboxLabeled("AllowDownedSRTS".Translate(), ref settings.allowEvenIfDowned);
                listing_Standard.CheckboxLabeled("AllowUnsecurePrisonerSRTS".Translate(), ref settings.allowEvenIfPrisonerUnsecured);
                listing_Standard.CheckboxLabeled("AllowCapturablePawnSRTS".Translate(), ref settings.allowCapturablePawns);

                listing_Standard.End();
            }

            if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
            {
                GraphicRequest graphicRequest = new GraphicRequest(props.referencedDef.graphicData.graphicClass, props.referencedDef.graphicData.texPath, ShaderTypeDefOf.Cutout.Shader, props.referencedDef.graphic.drawSize,
                       Color.white, Color.white, props.referencedDef.graphicData, 0, null, null);
                string texPath = props.referencedDef.graphicData.texPath;
                if (graphicRequest.graphicClass == typeof(Graphic_Multi))
                    texPath += "_north";

                Rect pictureRect = new Rect(inRect.width / 2, inRect.height / 3, 300f, 300f);
                GUI.DrawTexture(pictureRect, ContentFinder<Texture2D>.Get(texPath, true));
                DialogSettings.Draw_Label(new Rect(pictureRect.x, inRect.height / 3 - 60f, 300f, 100f), props.referencedDef.label.Replace("SRTS ", ""), Color.clear, Color.white, GameFont.Medium, TextAnchor.MiddleCenter);

                var valueFont = Text.Font;
                var alignment = Text.Anchor;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(inRect.width - settingsCategory.width, settingsCategory.y + (Prefs.DevMode ? 30f : 0f), settingsCategory.width, 30f), props.defaultValues ? "DefaultValues".Translate() : "CustomValues".Translate());
                Text.Font = valueFont;
                Text.Anchor = alignment;
            }
            if(props.defaultValues && !props.IsDefault)
            {
                props.defaultValues = false;
            }
            
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "SRTSExpanded".Translate();
        }

        internal void ResetMainSettings()
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            this.settings.passengerLimits = true;
            this.settings.dynamicWorldDrawingSRTS = true;
            this.settings.displayHomeItems = true;
            this.settings.allowEvenIfDowned = true;
            this.settings.allowEvenIfPrisonerUnsecured = false;
            this.settings.allowCapturablePawns = true;
            this.settings.disallowedBombs.Clear();
            this.settings.allowedBombs.Clear();
            SRTSHelper.PopulateAllowedBombs();
        }

        internal void ResetBombList()
        {
            this.settings.disallowedBombs.Clear();
            this.settings.allowedBombs.Clear();
            SRTSHelper.PopulateAllowedBombs();
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
            if(settings.defProperties is null || settings.defProperties.Count <= 0)
            {
                settings.defProperties = new Dictionary<string, SRTS_DefProperties>();
                foreach(ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.GetCompProperties<CompProperties_LaunchableSRTS>() != null))
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
                case SRTS.SettingsCategory.Research:
                    return "ResearchSettings".Translate();
            }
            Log.Warning("Setting Category " + category.ToString() + " not yet implemented. - Smash Phil");
            return category.ToString();
        }

        public static T GetStatFor<T>(string defName, StatName stat)
        {
            SRTSMod.mod.CheckDictionaryValid();
            if(!mod.settings.defProperties.ContainsKey(defName) && DefDatabase<ThingDef>.GetNamedSilentFail(defName)?.GetCompProperties<CompProperties_LaunchableSRTS>() != null)
            {
                Log.Warning("Key was not able to be found inside ModSettings Dictionary. Resetting to default values and initializing: " + defName);
                mod.settings.defProperties.Add(defName, new SRTS_DefProperties(DefDatabase<ThingDef>.GetNamed(defName))); //Initialize
            }

            switch(stat)
            {
                case StatName.massCapacity:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].massCapacity, typeof(T));
                case StatName.minPassengers:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].minPassengers, typeof(T));
                case StatName.maxPassengers:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].maxPassengers, typeof(T));
                case StatName.flightSpeed:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].flightSpeed, typeof(T));

                /* SOS2 Compatibility */
                case StatName.spaceFaring:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].spaceFaring, typeof(T));
                case StatName.shuttleBayLanding:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].shuttleBayLanding, typeof(T));
                /* ------------------ */

                case StatName.bombingSpeed:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].bombingSpeed, typeof(T));
                case StatName.numberBombs:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].numberBombs, typeof(T));
                case StatName.radiusDrop:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].radiusDrop, typeof(T));
                case StatName.distanceBetweenDrops:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].distanceBetweenDrops, typeof(T));
                case StatName.researchPoints:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].researchPoints, typeof(T));
                case StatName.fuelPerTile:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].fuelPerTile, typeof(T));
                case StatName.precisionBombingNumBombs:
                    return (T)Convert.ChangeType(mod.settings.defProperties[defName].precisionBombingNumBombs, typeof(T));
            }
            return default;
        }

        public string currentKey;

        private SettingsCategory currentPage;

        public Vector2 scrollPosition;

        public SRTS_DefProperties props;

        private bool checkValidityBombs = false;
    }

    public class SRTS_DefProperties : IExposable
    {
        public SRTS_DefProperties()
        {
        }

        public SRTS_DefProperties(ThingDef def)
        {
            this.referencedDef = def;
            this.defaultValues = true;
            this.massCapacity = referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
            this.minPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers;
            this.maxPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers;
            this.flightSpeed = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed;
            this.fuelPerTile = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().fuelPerTile;

            this.spaceFaring = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().spaceFaring;
            this.shuttleBayLanding = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().shuttleBayLanding;

            if (this.requiredResearch is null)
                this.requiredResearch = new List<ResearchProjectDef>();
            this.requiredResearch.AddRange(referencedDef.researchPrerequisites);
            this.researchPoints = this.RequiredResearch?[0].baseCost ?? 1;
            this.customResearchRequirements = new List<ResearchProjectDef>();
            this.customResearchDefNames = new List<string>();

            this.bombingCapable = referencedDef.GetCompProperties<CompProperties_BombsAway>() != null;
            if (BombCapable)
            {
                this.numberBombs = referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs;
                this.radiusDrop = referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop;
                this.bombingSpeed = referencedDef.GetCompProperties<CompProperties_BombsAway>().speed;
                this.distanceBetweenDrops = referencedDef.GetCompProperties<CompProperties_BombsAway>().distanceBetweenDrops;
                this.precisionBombingNumBombs = referencedDef.GetCompProperties<CompProperties_BombsAway>().precisionBombingNumBombs;
            }
        }

        public bool IsDefault
        {
            get
            {
                bool flag = true;
                if(BombCapable)
                {
                    flag = this.numberBombs == referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs && this.radiusDrop == referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop &&
                        this.precisionBombingNumBombs == referencedDef.GetCompProperties<CompProperties_BombsAway>().precisionBombingNumBombs && this.bombingSpeed == referencedDef.GetCompProperties<CompProperties_BombsAway>().speed 
                        && this.distanceBetweenDrops == referencedDef.GetCompProperties<CompProperties_BombsAway>().distanceBetweenDrops;
                }
                return flag && this.RequiredResearch[0].baseCost == this.researchPoints && !this.customResearchDefNames.Any() && this.massCapacity == referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity && this.minPassengers == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers
                    && this.maxPassengers == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers && this.flightSpeed == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed &&
                    this.fuelPerTile == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().fuelPerTile;
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

        public float fuelPerTile = 2.25f;

        public float researchPoints = 1f;

        public List<ResearchProjectDef> requiredResearch;

        public List<ResearchProjectDef> customResearchRequirements;

        private List<string> customResearchDefNames;

        /* SOS2 Compatibility */
        public bool spaceFaring;
        
        public bool shuttleBayLanding;
        /* ------------------ */


        public List<ResearchProjectDef> ResearchPrerequisites
        {
            get
            {
                if(this.requiredResearch is null)
                {
                    this.requiredResearch = new List<ResearchProjectDef>();
                    this.requiredResearch.AddRange(referencedDef.researchPrerequisites);
                }
                if (this.customResearchRequirements is null)
                    this.customResearchRequirements = new List<ResearchProjectDef>();
                List<ResearchProjectDef> projects = new List<ResearchProjectDef>();
                projects.AddRange(requiredResearch);
                projects.AddRange(customResearchRequirements);
                return projects;
            }
        }

        public List<ResearchProjectDef> RequiredResearch
        {
            get
            {
                if(this.requiredResearch is null || this.requiredResearch.Count <= 0)
                {
                    this.requiredResearch = new List<ResearchProjectDef>();
                    this.requiredResearch.AddRange(referencedDef.researchPrerequisites);
                }
                return requiredResearch;
            }
        }

        public List<ResearchProjectDef> CustomResearch
        {
            get
            {
                if(this.customResearchRequirements is null)
                    customResearchRequirements = new List<ResearchProjectDef>();
                if(this.customResearchDefNames is null)
                    customResearchDefNames = new List<string>();
                if(customResearchDefNames.Count != customResearchRequirements.Count)
                {
                    customResearchRequirements.Clear();
                    foreach(string defName in customResearchDefNames)
                    {
                        customResearchRequirements.Add(DefDatabase<ResearchProjectDef>.GetNamed(defName));
                    }
                }
                return customResearchRequirements;
            }
        }

        public void AddCustomResearch(ResearchProjectDef proj)
        {
            this.customResearchRequirements.Add(proj);
            this.customResearchDefNames.Add(proj.defName);
        }

        public void RemoveCustomResearch(ResearchProjectDef proj)
        {
            this.customResearchRequirements.Remove(proj);
            this.customResearchDefNames.Remove(proj.defName);
        }

        public void ResetCustomResearch()
        {
            if(this.customResearchRequirements is null || this.customResearchDefNames is null)
            {
                this.customResearchRequirements = new List<ResearchProjectDef>();
                this.customResearchDefNames = new List<string>();
            }
            this.customResearchRequirements.Clear();
            this.customResearchDefNames.Clear();
        }

        public void ResetToDefaultValues()
        {
            this.defaultValues = true;
            
            this.massCapacity = this.referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
            this.minPassengers = this.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers;
            this.maxPassengers = this.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers;
            this.flightSpeed = this.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed;
            this.fuelPerTile = this.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().fuelPerTile;

            this.spaceFaring = this.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().spaceFaring;
            this.shuttleBayLanding = this.referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().shuttleBayLanding;

            int num = 0;
            foreach (ResearchProjectDef proj in this.referencedDef.researchPrerequisites)
            {
                num += (int)proj.baseCost;
            }
            this.researchPoints = num;
            if (this.requiredResearch is null)
                this.requiredResearch = new List<ResearchProjectDef>();
            this.requiredResearch = this.referencedDef.researchPrerequisites;
            this.ResetCustomResearch();
            if (this.BombCapable)
            {
                this.bombingSpeed = this.referencedDef.GetCompProperties<CompProperties_BombsAway>().speed;
                this.numberBombs = this.referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs;
                this.radiusDrop = this.referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop;
                this.distanceBetweenDrops = referencedDef.GetCompProperties<CompProperties_BombsAway>().distanceBetweenDrops;
                this.precisionBombingNumBombs = this.referencedDef.GetCompProperties<CompProperties_BombsAway>().precisionBombingNumBombs;
            }
        }

        public bool ResetReferencedDef(string defName)
        {
            if(this.referencedDef is null)
            {
                referencedDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            }
            return referencedDef != null;
        }

        /// <summary>
        /// Bomb related values
        /// </summary>
        public bool BombCapable
        {
            get
            {
                return bombingCapable;
            }
        }
        public int numberBombs = 0;

        public int radiusDrop = 1;

        public float distanceBetweenDrops = 10f;

        public float bombingSpeed = 1;

        public int precisionBombingNumBombs = 1;

        private bool bombingCapable;

        public bool defaultValues = true;

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.defaultValues, "defaultValues");
            Scribe_Values.Look(ref this.massCapacity, "massCapacity");
            Scribe_Values.Look(ref this.minPassengers, "minPassengers");
            Scribe_Values.Look(ref this.maxPassengers, "maxPassengers");
            Scribe_Values.Look(ref this.flightSpeed, "flightSpeed");
            //Scribe_Values.Look(ref this.researchPoints, "researchPoints");
            Scribe_Values.Look(ref this.fuelPerTile, "fuelPerTile");

            Scribe_Values.Look(ref this.spaceFaring, "spaceFaring");
            Scribe_Values.Look(ref this.shuttleBayLanding, "shuttleBayLanding");

            //Scribe_Collections.Look<string>(ref customResearchDefNames, "customResearchDefNames", LookMode.Value, new object[0]); ;

            Scribe_Values.Look(ref this.bombingCapable, "bombingCapable");
            Scribe_Values.Look(ref this.numberBombs, "numberBombs");
            Scribe_Values.Look(ref this.radiusDrop, "radiusDrop");
            Scribe_Values.Look(ref this.distanceBetweenDrops, "distanceBetweenDrops");
            Scribe_Values.Look(ref this.bombingSpeed, "bombingSpeed");
            Scribe_Values.Look(ref this.precisionBombingNumBombs, "precisionBombingNumBombs");
        }
    }
}
