using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Text;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Payroll
{
    public class JobPositionGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public AccountingAccountGraphQLModel Account { get; set; } = new();


    }

    public class JobPositionCreateMessage
    {
        public UpsertResponseType<JobPositionGraphQLModel> CreatedJobPosition { get; set; }
    }

    public class JobPositionUpdateMessage
    {

        public UpsertResponseType<JobPositionGraphQLModel> UpdatedJobPosition { get; set; }
    }

    public class JobPositionDeleteMessage
    {
        public DeleteResponseType DeletedJobPosition { get; set; } = new();
    }
}
