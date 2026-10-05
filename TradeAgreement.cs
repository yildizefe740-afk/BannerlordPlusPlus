using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class TradeAgreement
    {
        [SaveableProperty(1)]
        public string Kingdom1Id { get; set; }

        [SaveableProperty(2)]
        public string Kingdom2Id { get; set; }

        [SaveableProperty(3)]
        public float TarrifReduction { get; set; }

        [SaveableProperty(4)]
        public double ExpirationDateDays { get; set; }

        public TradeAgreement() { }

        public TradeAgreement(Kingdom k1, Kingdom k2, float reduction)
        {
            Kingdom1Id = k1?.StringId;
            Kingdom2Id = k2?.StringId;
            TarrifReduction = reduction;
            ExpirationDateDays = CampaignTime.Now.ToDays + 100;
        }

        public bool IsExpired => CampaignTime.Now.ToDays >= ExpirationDateDays;

        public Kingdom Kingdom1 => Kingdom.All.FirstOrDefault(k => k != null && k.StringId == Kingdom1Id);
        public Kingdom Kingdom2 => Kingdom.All.FirstOrDefault(k => k != null && k.StringId == Kingdom2Id);
    }
}