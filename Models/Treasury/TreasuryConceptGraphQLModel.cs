using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Treasury
{
    public class TreasuryConceptGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int AccountingAccountId { get; set; } = 0;
        public bool AllowMargin { get; set; } = false;
        public decimal Margin { get; set; } = 0;
        public int MarginBasis { get; set; } = 0;
        public string Type { get; set; } = string.Empty;
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();

        public override string ToString()
        {
            return Name;
        }
        public class TreasuryConceptCreateMessage
        {
            public required UpsertResponseType<TreasuryConceptGraphQLModel> CreatedTreasuryConcept { get; set; }        
        }

        public class TreasuryConceptUpdateMessage
        {
            public required UpsertResponseType<TreasuryConceptGraphQLModel> UpdatedTreasuryConcept { get; set; }
        }

        public class TreasuryConceptDeleteMessage
        {
            public required DeleteResponseType DeletedTreasuryConcept { get; set; }
        }
    }
}
