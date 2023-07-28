using UnityEngine;
using Menu.Remix.MixedUI;
using System;

namespace ElectricRubbish
{
    public class ElectricRubbishOptions : OptionInterface
    {
        static Configurable<int> Percent_Rock_Replace_Rate;
        public static float RockReplaceRate => Percent_Rock_Replace_Rate.Value / 100f;
        static Configurable<bool> All_Rubbish_Rechargable;
        public static bool AllRubbishRechargeable => All_Rubbish_Rechargable.Value;
        public enum LETHALITY
        {
            Shock_Only,
            Kills_Artificer,
            Kills_Anything
        }
        static Configurable<string> Overcharge_Lethality;
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
         

        public ElectricRubbishOptions()
        {
            Percent_Rock_Replace_Rate = config.Bind<int>("Percent_Rock_Replace_Rate", 10, new ConfigurableInfo("When set to 1, all rubbish will be electrified."));
            All_Rubbish_Rechargable = config.Bind<bool>("All_Rubbish_Rechargable", false, new ConfigurableInfo("When true, all rubbish is converted into chargeable rubbish."));
            Overcharge_Lethality = config.Bind<string>("Overcharge_Lethality", "Kills Artificer", new ConfigAcceptableList<string>(new string[]{ "Shock Only", "Kills Artificer", "Kills Anything" }));
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
            OpLabel Label2 = new OpLabel(0f, 470f, "Recharge any Rubbish");
            OpCheckBox checkbox = new OpCheckBox(All_Rubbish_Rechargable, new Vector2(0f, 440f));
            checkbox.description = "Any rubbish not charged at the start of a cycle can still be charged manually.";

            OpLabel Label3 = new OpLabel(0f, 390f, "Mishandle Lethality");
            OpListBox listbox = new OpListBox(Overcharge_Lethality, new Vector2(0f, 280f), 100, 3);
            listbox.description = "Choose how overcharged rubbish affects creatures that come into contact with it.";
            listbox._itemList[0].desc = "Stuns the creature for a short time.";
            listbox._itemList[1].desc = "Improper handling causes artificer to explode.";
            listbox._itemList[2].desc = "Deals enough damage to instantly kill any slugcat...";


            Tabs[0].AddItems(new UIelement[]
            {
                    Label, slider, Label2, checkbox, Label3, listbox
            });
        }
    }
}
