using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Treasury
{
    public class ConceptGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int AccountingAccountId { get; set; } = 0;
        public bool AllowMargin { get; set; } = false;
        public decimal Margin { get; set; } = 0m;
        public int MarginBasis { get; set; } = 0;
        public string Type { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name;
        }
        public class ConceptCreateMessage
        {
            public ConceptGraphQLModel CreatedTreasuryConcept { get; set; } = new ConceptGraphQLModel();
        }

        public class TreasuryConceptUpdateMessage
        {
            public ConceptGraphQLModel UpdatedTreasuryConcept { get; set; } = new ConceptGraphQLModel();
        }

        public class TreasuryConceptDeleteMessage
        {
            public ConceptGraphQLModel DeletedTreasuryConcept { get; set; } = new ConceptGraphQLModel();
        }
    }
}
