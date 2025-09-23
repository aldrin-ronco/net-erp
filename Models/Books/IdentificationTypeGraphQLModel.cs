using Models.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class IdentificationTypeGraphQLModel
    {
        public int Id { get; set; } = 0;

        // Codigo del tipo de documento establecido por la DIAN 
        public string Code { get; set; } = string.Empty;

        // Nombre del tipo de documento
        public string Name { get; set; } = string.Empty;

        // Requiere digito de verificacion
        public bool HasVerificationDigit { get; set; } = false;

        // Longitud minima del documento
        public int MinimumDocumentLength { get; set; } = 7;

        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }
    public class IdentificationTypeDTO : IdentificationTypeGraphQLModel
    {
        private bool _isChecked;

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                }
            }
        }
    }
    public class IdentificationTypeCreateMessage
    {
        public UpsertResponseType<IdentificationTypeGraphQLModel> CreatedIdentificationType { get; set; } = new();
    }

    public class IdentificationTypeUpdateMessage
    {
        public UpsertResponseType<IdentificationTypeGraphQLModel> UpdatedIdentificationType { get; set; } = new();
    }

    public class IdentificationTypeDeleteMessage
    {
        public DeleteResponseType DeletedIdentificationType { get; set; } = new();
    }
}
