using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace BannerlordPlusPlus
{
    public class DefensivePact
    {
        [SaveableField(1)]
        public string Kingdom1Id;

        [SaveableField(2)]
        public string Kingdom2Id;

        [SaveableField(3)]
        public double ExpirationDateDays;

        [SaveableField(4)]
        public bool IsCalled;

        // BOŞ CONSTRUCTOR
        public DefensivePact() { }

        public DefensivePact(Kingdom k1, Kingdom k2, double durationDays = 100)
        {
            Kingdom1Id = k1.StringId;
            Kingdom2Id = k2.StringId;
            ExpirationDateDays = CampaignTime.Now.ToDays + durationDays;
            IsCalled = false;
        }

        public bool IsExpired => CampaignTime.Now.ToDays >= ExpirationDateDays;
    }
}