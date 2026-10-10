using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class MarriagePact
    {
        [SaveableProperty(1)]
        public Kingdom Kingdom1 { get; set; }

        [SaveableProperty(2)]
        public Kingdom Kingdom2 { get; set; }

        [SaveableProperty(3)]
        public Hero Hero1 { get; set; }

        [SaveableProperty(4)]
        public Hero Hero2 { get; set; }

        public MarriagePact() { }

        public MarriagePact(Kingdom k1, Kingdom k2, Hero h1, Hero h2)
        {
            Kingdom1 = k1;
            Kingdom2 = k2;
            Hero1 = h1;
            Hero2 = h2;
        }

        public bool IsValid
        {
            get
            {
                if (Hero1 == null || Hero2 == null || !Hero1.IsAlive || !Hero2.IsAlive)
                    return false;

                if (Hero1.Spouse != Hero2)
                    return false;

                var clan1 = Hero1.Clan;
                var rulingClan1 = Kingdom1?.RulingClan;
                if (rulingClan1 != null && clan1 != rulingClan1)
                    return false;

                var clan2 = Hero2.Clan;
                var rulingClan2 = Kingdom2?.RulingClan;
                if (rulingClan2 != null && clan2 != rulingClan2)
                    return false;

                return true;
            }
        }
    }
}