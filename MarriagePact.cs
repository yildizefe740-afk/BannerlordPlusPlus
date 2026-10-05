using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class MarriagePact
    {
        [SaveableProperty(1)]
        public Kingdom Kingdom1{get; set;}

        [SaveableProperty(2)]
        public Kingdom Kingdom2{get; set;}

        [SaveableProperty(3)]
        public Hero Hero1{get; set;}

        [SaveableProperty(4)]
        public Hero Hero2{get; set;}

        public MarriagePact(){}

        public MarriagePact(Kingdom k1, Kingdom k2, Hero h1, Hero h2)
        {
            Kingdom1 = k1;
            Kingdom2 = k2;
            Hero1 = h1;
            Hero2 = h2;
        }

        public bool IsValid =>
            Hero1 != null &&
            Hero2 != null &&
            Hero1.IsAlive &&
            Hero2.IsAlive &&
            Hero1.Spouse == Hero2 &&
            Hero1.Clan == Kingdom1?.RulingClan &&
            Hero2.Clan == Kingdom2?.RulingClan;
    }
}