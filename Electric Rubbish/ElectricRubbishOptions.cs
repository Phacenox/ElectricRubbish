using UnityEngine;

namespace ElectricRubbish
{
    public class ElectricRubbishOptions : OptionInterface
    {
        static Configurable<int> Percent_Rock_Replace_Rate;
        public static float rockReplaceRate => Percent_Rock_Replace_Rate.Value / 100f;
        static Configurable<bool> All_Rubbish_Rechargable;
        public static bool allRubbishRechargeable => All_Rubbish_Rechargable.Value;

        public ElectricRubbishOptions()
        {
            Percent_Rock_Replace_Rate = config.Bind<int>("Percent_Rock_Replace_Rate", 10, new ConfigurableInfo("When set to 1, all rubbish will be electrified."));
            All_Rubbish_Rechargable = config.Bind<bool>("All_Rubbish_Rechargable", false, new ConfigurableInfo("When true, all rubbish is converted into chargeable rubbish."));
        }

        public override void Initialize()
        {
            base.Initialize();
            this.Tabs = new[]
            {
                    new Menu.Remix.MixedUI.OpTab(this)
                };
            Menu.Remix.MixedUI.OpLabel Label = new Menu.Remix.MixedUI.OpLabel(0f, 550f, "Percent Natural Electrification Rate");
            Menu.Remix.MixedUI.OpSlider slider = new Menu.Remix.MixedUI.OpSlider(Percent_Rock_Replace_Rate, new Vector2(0f, 520f), 200)
            {
                min = 0,
                max = 100
            };
            Menu.Remix.MixedUI.OpLabel Label2 = new Menu.Remix.MixedUI.OpLabel(0f, 450f, "Recharge any Rubbish");
            Menu.Remix.MixedUI.OpCheckBox checkbox = new Menu.Remix.MixedUI.OpCheckBox(All_Rubbish_Rechargable, new Vector2(0f, 420f));

            Tabs[0].AddItems(new Menu.Remix.MixedUI.UIelement[]
            {
                    Label, slider, Label2, checkbox
            });
        }
    }
}
