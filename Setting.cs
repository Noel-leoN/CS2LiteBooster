using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace GameLiteBooster
{
    [FileLocation(nameof(GameLiteBooster))]
    [SettingsUIGroupOrder(kOptionsGroup, kButtonGroup)]
    [SettingsUIShowGroupName(kOptionsGroup, kButtonGroup)]

    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kOptionsGroup = "Options";
        public const string kButtonGroup = "Actions";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }        

        [SettingsUISection(kSection, kOptionsGroup)]
        public bool Logging { get; set; }

        [SettingsUISection(kSection, kOptionsGroup)]
        public bool DisablePetSystem { get; set; }

        [SettingsUISection(kSection, kOptionsGroup)]
        public bool DisableRamdonTraffic { get; set; }

        [SettingsUISection(kSection, kOptionsGroup)]
        public bool DisableTaxiDispatch { get; set; }

        //[SettingsUISection(kSection, kOptionsGroup)]
        //public bool DisableRentAdjust { get; set; }

        public override void SetDefaults()
        {

            Logging = false;

            DisablePetSystem = true;

            DisableRamdonTraffic = true;

            DisableTaxiDispatch = true;

            //DisableRentAdjust = false;

        }

    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
        {
            { m_Setting.GetSettingsLocaleID(),$"{Mod.ModName} beta" },
            { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

            { m_Setting.GetOptionGroupLocaleID(Setting.kOptionsGroup), "Options" },
            { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Logging)), "Detailed logging" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.Logging)), "Outputs more diagnostics information to the log file." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisablePetSystem)), "Disable all the animal-related systems.(Need Restart)" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisablePetSystem)), "Disable all the animal-related(including wild animals) systems.(Need Restart)" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableRamdonTraffic)), "DisableRamdonTraffic(Test,Need Restart)" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableRamdonTraffic)), "DisableRamdonTraffic,This will slightly improve performance,Generally does not affect the economy.(Test,Need Restart)." },                        

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTaxiDispatch)), "Disable Taxi Dispatch(Test,Need Restart)" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTaxiDispatch)), "Disable Taxi Dispatch,This will improve performance to a certain extent. It is currently being tested and it is not yet known whether it will affect the economy.(Test,Need Restart)." },

            //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableRentAdjust)), "Disable Rent calculation. (not available for now)" },
            //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableRentAdjust)), "Disable Rent calculation,This will have a greater impact on performance.(not available for now)." },


        };
        }



        public void Unload()
        {
        }

    }


    public class LocaleCN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleCN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
            { m_Setting.GetSettingsLocaleID(),$"{Mod.ModNameCN} 测试版" },
            { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

            { m_Setting.GetOptionGroupLocaleID(Setting.kOptionsGroup), "Options" },
            { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Logging)), "详细日志" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.Logging)), "输出详细诊断日志." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisablePetSystem)), "关闭所有动物相关计算系统(需要重启)" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisablePetSystem)), "关闭所有动物(包含宠物狗)相关计算系统(需要重启游戏)." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableRamdonTraffic)), "关闭随机虚拟交通流量(需要重启)" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableRamdonTraffic)), "关闭过境车船飞机和随机生成的市内交通等与经济无关的虚拟交通流量.会轻微改善性能.一般不影响经济.(需要重启游戏)." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTaxiDispatch)), "关闭出租车派车系统(需要重启)" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTaxiDispatch)), "关闭出租车派车系统,会一定程度改善性能，尚在测试中不知是否影响经济.(需要重启游戏)." },

            //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableRentAdjust)), "简化租金计算(暂不可用)" },
            //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableRentAdjust)), "简化房屋租金计算方式，会较大改善性能.(暂不可用)." },

            };
        }
        public void Unload()
        {
        }

    }
}
