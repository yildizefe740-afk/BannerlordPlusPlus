using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class SaveDefiner : SaveableTypeDefiner
    {
        // Moduna özel benzersiz bir ID bloğu (örneğin 1_890_100)
        public SaveDefiner() : base(1_890_100) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(SubjectAttributes), 1);
            AddClassDefinition(typeof(DefensivePact), 2);
            AddClassDefinition(typeof(TradeAgreement), 3);
        }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(SubjectType), 4);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, SubjectAttributes>));
            ConstructContainerDefinition(typeof(List<DefensivePact>));
            ConstructContainerDefinition(typeof(List<TradeAgreement>));
        }
    }
}