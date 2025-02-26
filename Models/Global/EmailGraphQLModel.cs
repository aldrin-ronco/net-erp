using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class EmailGraphQLModel
    {
        public string Id { set; get; }
        public string Description { set; get; } = string.Empty;
        public string Email { set; get; } = string.Empty;
        public string Password { set; get; } = string.Empty;
        public bool IsCorporate { set; get; } = true;
        public bool SendElectronicInvoice { set; get; } = false;
        public AccountingEntityGraphQLModel AccountingEntity { get; set; }
        public SmtpGraphQLModel Smtp { get; set; }
        public AwsSesGraphQLModel AwsSes { get; set; }
        public override string ToString()
        {
            return Description;
        }

        public class EmailDeleteMessage
        {
            public EmailGraphQLModel DeleteEmail;

            public EmailGraphQLModel DeletedEmail { set; get; } = new EmailGraphQLModel();
        }

        public class EmailCreateMessage
        {
            public EmailGraphQLModel CreatedEmail { set; get; } = new EmailGraphQLModel();
        }

        public class EmailUpdateMessage
        {
            public EmailGraphQLModel UpdatedEmail { set; get; } = new EmailGraphQLModel();
        }         

    }
}
