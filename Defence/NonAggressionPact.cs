using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class NonAggressionPact
    {
        [SaveableField(1)]
        public string Kingdom1Id;
        [SaveableField(2)]
        public string Kingdom2Id;
        [SaveableField(3)]
        public double ExpirationDateDays;
        [SaveableField(4)]
        public bool HasPact;

        public NonAggressionPact()
        {
            //for save empty constructor 
        }

        public NonAggressionPact(Kingdom k1, Kingdom k2, double durationDays)
        {
            Kingdom1Id = k1.StringId;
            Kingdom2Id = k2.StringId;
            ExpirationDateDays = CampaignTime.Now.ToDays + durationDays;
            HasPact = true;
        }

         public bool IsExpired => CampaignTime.Now.ToDays >= ExpirationDateDays;

        
    }
}