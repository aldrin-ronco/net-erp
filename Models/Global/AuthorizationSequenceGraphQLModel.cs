using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class AuthorizationSequenceGraphQLModel
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public char Mode { get; set; }
        public string TechnicalKey { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Prefix { get; set; } = string.Empty;

        public int StartRange { get; set; }
        public int EndRange { get; set; }
        public int AuthorizationSequenceTypeId { get; set; }
        public int CostCenterId { get; set; }
        public int CurrentInvoiceNumber { get; set; }
        public AuthorizationSequenceGraphQLModel? NextAuthorizationSequence { get; set; }

        public AuthorizationSequenceTypeGraphQLModel? AuthorizationSequenceType { get; set; }
        public CostCenterGraphQLModel? CostCenter { get; set; }
        public CostCenterGraphQLModel? AuthorizationSequenceByCostCenter { get; set; }
        

    }


    public class AuthorizationSequenceCreateMessage
    {
        
        public UpsertResponseType<AuthorizationSequenceGraphQLModel> CreatedAuthorizationSequence { get; set; }
    }
    public class AuthorizationSequenceDeleteMessage
    {
        public DeleteResponseType DeletedAuthorizationSequence { get; set; }
    }

    public class AuthorizationSequenceUpdateMessage
    {
        public UpsertResponseType<AuthorizationSequenceGraphQLModel> UpdatedAuthorizationSequence { get; set; }
    }
    public class AuthorizationSequenceDetailDataContext
    {
        
        public PageType<AuthorizationSequenceTypeGraphQLModel> AuthorizationSequenceTypes { get; set; }
        public PageType<AuthorizationSequenceGraphQLModel> AuthorizationSequences { get; set; }
    }
    public class AuthorizationSequenceDataContext
    {
        public PageType<AuthorizationSequenceGraphQLModel> AuthorizationSequences { get; set; }
        public PageType<CostCenterGraphQLModel> CostCenters { get; set; }
    }
    public class AuthorizationSequenceResponse
    {
        public ObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
    }

}
