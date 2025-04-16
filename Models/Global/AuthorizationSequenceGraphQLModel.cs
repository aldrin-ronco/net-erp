using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Prefix { get; set; } = string.Empty;

        public int StartRange { get; set; }
        public int EndRange { get; set; }
        public int AuthorizationSequenceTypeId { get; set; }
        public int CostCenterId { get; set; }
        public string CurrentInvoceNumber { get; set; } = string.Empty;

        public AuthorizationSequenceTypeGraphQLModel AuthorizationSequenceType { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }

    }


    public class AuthorizationSequenceCreateMessage
    {
        public AuthorizationSequenceGraphQLModel CreatedAuthorizationSequence { get; set; }
        public ObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences { get; set; }
    }
    public class AuthorizationSequenceDeleteMessage
    {
        public AuthorizationSequenceGraphQLModel DeletedAuthorizationSequence { get; set; }
    }

    public class AuthorizationSequenceUpdateMessage
    {
        public AuthorizationSequenceGraphQLModel UpdatedAuthorizationSequence { get; set; }
        public ObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences { get; set; }
    }
    public class AuthorizationSequenceDataContext
    {
        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AuthorizationSequenceTypes { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; }
    }
}
