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
        public AccountingAccountGraphQLModel AccountingAccount { get; set; }

        public override string ToString()
        {
            return Name;
        }
        public class TreasuryConceptCreateMessage
        {
            public UpsertResponseType<TreasuryConceptGraphQLModel> CreatedTreasuryConcept { get; set; } = new ();
        }

        public class TreasuryConceptUpdateMessage
        {
            public UpsertResponseType<TreasuryConceptGraphQLModel> UpdatedTreasuryConcept { get; set; } = new ();
        }

        public class TreasuryConceptDeleteMessage
        {
            public DeleteResponseType DeletedTreasuryConcept { get; set; } = new();
        }
    }
}
