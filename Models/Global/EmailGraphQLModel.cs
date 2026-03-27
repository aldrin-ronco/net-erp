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
        public bool IsCorporate { set; get; } = true;
        public bool isElectronicInvoiceRecipient { set; get; } = false;
        public SmtpGraphQLModel Smtp { get; set; }
        public override string ToString()
        {
            return Description;
        }

        public class EmailDeleteMessage
        {

            public DeleteResponseType DeletedEmail { set; get; } = new();
        }

        public class EmailCreateMessage
        {
            public UpsertResponseType<EmailGraphQLModel> CreatedEmail { set; get; } = new();
        }

        public class EmailUpdateMessage
        {
            public UpsertResponseType<EmailGraphQLModel> UpdatedEmail { set; get; } = new();
        }         

    }
}
