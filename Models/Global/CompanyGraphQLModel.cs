using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class CompanyGraphQLModel
    {
        public int Id { get; set; }
        public AccountingEntityGraphQLModel CompanyEntity { get; set; } = new();
        public IEnumerable<CompanyLocationGraphQLModel> CompanyLocations { get; set; } = [];
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

    }

    public class CompanyCreateMessage
    {
        public UpsertResponseType<CompanyGraphQLModel> CreatedCompany { get; set; } = new();
    }

    public class CompanyUpdateMessage
    {
        public UpsertResponseType<CompanyGraphQLModel> UpdatedCompany { get; set; } = new();
    }

    public class CompanyDeleteMessage
    {
        public DeleteResponseType DeletedCompany { get; set; } = new();
    }
}
