using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace BannerlordPlusPlus
{
    public class TradeAgreement
    {
        [SaveableField(1)]
        public string Kingdom1Id;

        [SaveableField(2)]
        public string Kingdom2Id;

        [SaveableField(3)]
        public float DiscountRate;

        [SaveableField(4)]
        public double ExpirationDateDays;

        // Bannerlord SaveSystem için BOŞ CONSTRUCTOR ZORUNLUDUR!
        public TradeAgreement() { }

        public TradeAgreement(Kingdom k1, Kingdom k2, float discountRate, double durationDays)
        {
            Kingdom1Id = k1.StringId;
            Kingdom2Id = k2.StringId;
            DiscountRate = discountRate;
            ExpirationDateDays = CampaignTime.Now.ToDays + durationDays;
        }

        public bool IsExpired => CampaignTime.Now.ToDays >= ExpirationDateDays;
    }
}