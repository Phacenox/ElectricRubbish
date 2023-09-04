using UnityEngine;
using Menu.Remix.MixedUI;
using System;

namespace ElectricRubbish
{
    public class ElectricRubbishOptions : OptionInterface
    {
        #region impulse_mod_overrides
        public static float replaceRateScalar = 1f;
        #endregion

        public static Configurable<int> Percent_Rock_Replace_Rate;
        public static float RockReplaceRate
        {
            get
            {
                return Percent_Rock_Replace_Rate.Value / 100f * replaceRateScalar;
            }
        }
            
        public static Configurable<bool> All_Rubbish_Rechargable;
        public static bool AllRubbishRechargeable => All_Rubbish_Rechargable.Value;
        public enum LETHALITY
        {
            Shock_Only,
            Kills_Artificer,
            Kills_Anything
        }
        public static Configurable<string> Overcharge_Lethality;
        public static LETHALITY OverchargeLethatlity
        {
            get
            {
                switch (Overcharge_Lethality.Value)
                {
                    case "Shock Only":
                        return LETHALITY.Shock_Only;
                    case "Kills Artificer":
                        return LETHALITY.Kills_Artificer;
                    case "Kills Anything":
                        return LETHALITY.Kills_Anything;
                }
                return LETHALITY.Shock_Only;
            }
        }
        public static Configurable<bool> Strong_Grip;
        public static bool StrongGrip => Strong_Grip.Value;
         

        public ElectricRubbishOptions()
        {
            Percent_Rock_Replace_Rate = config.Bind<int>("Percent_Rock_Replace_Rate", 8, new ConfigurableInfo("When set to 1, all rubbish will be electrified."));
            All_Rubbish_Rechargable = config.Bind<bool>("All_Rubbish_Rechargable", false, new ConfigurableInfo("When true, all rubbish is converted into chargeable rubbish."));
            Overcharge_Lethality = config.Bind<string>("Overcharge_Lethality", "Kills Artificer", new ConfigAcceptableList<string>(new string[]{ "Shock Only", "Kills Artificer", "Kills Anything" }));
            Strong_Grip = config.Bind<bool>("Strong_Grip", true);
        }

        public override void Initialize()
        {
            base.Initialize();
            this.Tabs = new[]
            {
                    new OpTab(this)
                };
            OpLabel Label = new OpLabel(0f, 550f, "Percent Natural Electrification Rate");
            OpSlider slider = new OpSlider(Percent_Rock_Replace_Rate, new Vector2(0f, 520f), 100)
            {
                min = 0,
                max = 100,
                description = "Choose how often charged rubbish spawns instead of normal rubbish."
            };
            OpLabel Label2 = new OpLabel(0f, 480f, "Recharge any Rubbish");
            OpCheckBox checkbox = new OpCheckBox(All_Rubbish_Rechargable, new Vector2(0f, 450f));
            checkbox.description = "Any rubbish not charged at the start of a cycle can still be charged using other electricity sources.";

            OpLabel Label4 = new OpLabel(00f, 410f, "Strong Grip");
            OpCheckBox checkbox2 = new OpCheckBox(Strong_Grip, new Vector2(0f, 380f));
            checkbox2.description = "Trying to pick up overcharged rubbish doesn't drop other held items. Spearmaster approved.";

            OpLabel Label3 = new OpLabel(0f, 340f, "Mishandle Lethality");
            OpListBox listbox = new OpListBox(Overcharge_Lethality, new Vector2(0f, 240f), 100, 3);
            listbox.description = "Choose how overcharged rubbish affects creatures that come into contact with it.";
            listbox._itemList[0].desc = "Stuns the creature for a short time.";
            listbox._itemList[1].desc = "Improper handling causes artificer to explode.";
            listbox._itemList[2].desc = "Deals enough damage to instantly kill any slugcat...";


            Tabs[0].AddItems(new UIelement[]
            {
                    Label, slider, Label2, checkbox, Label4, checkbox2, Label3, listbox
            });
        }
    }
}
