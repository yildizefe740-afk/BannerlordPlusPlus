using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class Alliance
    {
        [SaveableField(1)]
        public string kingdom1ID;
        [SaveableField(2)]
        public string kingdom2ID;
        [SaveableField(3)]
        public bool IsCalled;
        [SaveableField(4)]
        public  float Trust = 50f; //breaking condition

        public Alliance(){} // for save

        public Alliance(Kingdom k1, Kingdom k2)
        {
            kingdom1ID = k1.StringId;
            kingdom2ID = k2.StringId;
            
        }

        public bool CanCall => !IsCalled;
    }
}