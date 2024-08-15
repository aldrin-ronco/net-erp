using Common.Extensions;
using Common.Interfaces;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class DocumentSequenceMasterGraphQLModel
    {
        public int Id { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; } = new();
        public string Number { get; set; } = string.Empty;
        public DateTime InitialDate { get; set; }
        public DateTime FinalDate { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int InitialNumber { get; set; }
        public int FinalNumber { get; set; }
        public string TitleLabel { get; set; } = string.Empty;
        public string SequenceLabel { get; set; } = string.Empty;
        public string AuthorizationType { get; set; } = string.Empty;
        public string AuthorizationKind { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string TechnicalKey { get; set; } = string.Empty;    
        public string State => $"{(IsActive ? "Activa" : "Inactiva")}";
        public DocumentSequenceDetailGraphQLModel DocumentSequenceDetail { get; set; } = new();
        public string Description => $"{TitleLabel} {Number} de {InitialDate.Date.ToShortDateString()}, Prefijo : {Prefix} del {InitialNumber} al {FinalNumber}".RemoveExtraSpaces();
    }

    public class DocumentSequenceCreateMessage
    {
        public DocumentSequenceMasterGraphQLModel CreatedDocumentSequence { get; set; } = new();
    }

    public class DocumentSequenceUpdateMessage
    {
        public DocumentSequenceMasterGraphQLModel UpdatedDocumentSequence { get; set; } = new();
    }

    public class DocumentSequenceDeleteMessage
    {
        public DocumentSequenceMasterGraphQLModel DeletedDocumentSequence { get; set; } = new();
    }

    public class DocumentSequenceDataContext
    {
        public IGenericDataAccess<DocumentSequenceMasterGraphQLModel>.PageType DocumentSequenceMasterPage { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; }
    }
}
