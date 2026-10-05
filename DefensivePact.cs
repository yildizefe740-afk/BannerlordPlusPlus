using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class DefensivePact
    {
        [SaveableProperty(1)]
        public string Kingdom1Id { get; set; }

        [SaveableProperty(2)]
        public string Kingdom2Id { get; set; }

        [SaveableProperty(3)]
        public double ExpirationDateDays { get; set; }

        [SaveableProperty(4)]
        public bool IsCalled { get; set; }

        // Save/Load ve Deserialization için boş constructor şart
        public DefensivePact() { }

        public DefensivePact(Kingdom k1, Kingdom k2, double durationDays = 100)
{
        Kingdom1Id = k1.StringId;
        Kingdom2Id = k2.StringId;
        ExpirationDateDays = CampaignTime.Now.ToDays + durationDays;
        IsCalled = false;
}

        // Anlık olarak hesaplanan Dinamik Property'ler
        public bool IsExpired => CampaignTime.Now.ToDays >= ExpirationDateDays;
        
        public bool CanCall => !IsCalled && !IsExpired;
    }
}