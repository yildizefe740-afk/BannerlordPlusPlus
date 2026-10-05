using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class SaveDefiner : SaveableTypeDefiner
    {
        public SaveDefiner() : base(1_890_100) { }

        protected override void DefineClassTypes()
        {
            // Kaydedilen TÜM özel sınıflar burada tanımlı olmalı
            AddClassDefinition(typeof(SubjectAttributes), 1);
            AddClassDefinition(typeof(DefensivePact), 2);
            AddClassDefinition(typeof(TradeAgreement), 3);
            AddClassDefinition(typeof(PeaceAgreement), 4);
            AddClassDefinition(typeof(MarriagePact), 5);
        }

        protected override void DefineEnumTypes()
        {
            // Enum türleri ayrı ID sırasıyla tanımlanır
            AddEnumDefinition(typeof(SubjectType), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            // SyncData içindeki Liste ve Sözlük yapıları
            ConstructContainerDefinition(typeof(Dictionary<string, SubjectAttributes>));
            ConstructContainerDefinition(typeof(List<DefensivePact>));
            ConstructContainerDefinition(typeof(List<TradeAgreement>));
            ConstructContainerDefinition(typeof(List<PeaceAgreement>));
            ConstructContainerDefinition(typeof(List<MarriagePact>));
            ConstructContainerDefinition(typeof(List<SubjectAttributes>));
        }
    }
}