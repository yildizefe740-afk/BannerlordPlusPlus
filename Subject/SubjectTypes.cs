using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public enum SubjectType
    {
        None,
        Vassal,     // Tariff and war contribution
        Tributary,  // Just income no war contribution
        March       // No income just soldiers
    }

    public class SubjectAttributes
    {
        [SaveableProperty(1)]
        public string SeniorKingdomId { get; set; }

        [SaveableProperty(2)]
        public string JuniorKingdomId { get; set; }

        [SaveableProperty(3)]
        public SubjectType Type { get; set; }

        [SaveableProperty(4)]
        public float LibertyDesire { get; set; }

        [SaveableProperty(5)]
        public bool CanDeclareWar { get; set; }

        [SaveableProperty(6)]
        public int TributAmount { get; set; }

        [SaveableProperty(7)]
        public bool BlockAgreements { get; set; }

        [SaveableProperty(8)]
        public bool IsRebelling { get; set; }

        // Helper Property'ler (Save/Load'u bozmadan nesnelere güvenle erişim sağlar)
        public Kingdom SeniorKingdom => Kingdom.All.FirstOrDefault(k => k.StringId == SeniorKingdomId);
        public Kingdom JuniorKingdom => Kingdom.All.FirstOrDefault(k => k.StringId == JuniorKingdomId);

        public SubjectAttributes() { }

        public SubjectAttributes(Kingdom senior, Kingdom junior, SubjectType type)
        {
            SeniorKingdomId = senior.StringId;
            JuniorKingdomId = junior.StringId;
            Type = type;
            LibertyDesire = 0f;
            IsRebelling = false;
        }
    }
}