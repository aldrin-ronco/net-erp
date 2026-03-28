using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class EmailGraphQLModel
    {
        public int Id { set; get; }
        public string Description { set; get; } = string.Empty;
        public string Email { set; get; } = string.Empty;
        public string Password { set; get; } = string.Empty;
        public bool IsActive { set; get; } = true;
        public bool IsCorporate { set; get; } = true;
        public bool IsElectronicInvoiceRecipient { set; get; } = false;
        public SmtpGraphQLModel? Smtp { get; set; }
        public override string ToString()
        {
            return Description;
        }

        public class EmailDeleteMessage
        {
            public required DeleteResponseType DeletedEmail { set; get; }
        }

        public class EmailCreateMessage
        {
            public required UpsertResponseType<EmailGraphQLModel> CreatedEmail { set; get; }
        }

        public class EmailUpdateMessage
        {
            public required UpsertResponseType<EmailGraphQLModel> UpdatedEmail { set; get; }
        }         

    }
}
