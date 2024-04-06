using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }

    public class CanDeleteIdentificationType
    {
        public bool CanDelete { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }
}
