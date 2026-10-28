using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class SaveDefiner : SaveableTypeDefiner
    {
        public SaveDefiner() : base(1_890_100) { }

        protected override void DefineClassTypes()
        {
            // Assign unique IDs to classes and enums in sequence.
            AddClassDefinition(typeof(SubjectAttributes), 1);
            AddClassDefinition(typeof(DefensivePact), 2);
            AddClassDefinition(typeof(TradeAgreement), 3);
            AddClassDefinition(typeof(PeaceAgreement), 4);
            AddClassDefinition(typeof(MarriagePact), 5);
            AddClassDefinition(typeof(NonAggressionPact), 8);
            AddClassDefinition(typeof(Alliance), 9);
        }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(SubjectType), 6);
            AddEnumDefinition(typeof(PeaceDealOptions), 7); 
        }

        protected override void DefineContainerDefinitions()
        {
            // Collection types used by SyncData.
            ConstructContainerDefinition(typeof(Dictionary<string, SubjectAttributes>));
            ConstructContainerDefinition(typeof(Dictionary<string, string>));
            ConstructContainerDefinition(typeof(List<DefensivePact>));
            ConstructContainerDefinition(typeof(List<TradeAgreement>));
            ConstructContainerDefinition(typeof(List<PeaceAgreement>));
            ConstructContainerDefinition(typeof(List<MarriagePact>));
            ConstructContainerDefinition(typeof(List<NonAggressionPact>));
            ConstructContainerDefinition(typeof(List<Alliance>));
            ConstructContainerDefinition(typeof(List<SubjectAttributes>));
        }
    }
}