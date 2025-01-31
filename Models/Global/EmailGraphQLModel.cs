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
        public string Name { set; get; } = string.Empty;
        public string Email { set; get; } = string.Empty;
        public string Password { set; get; } = string.Empty;
        public bool IsCorporate { set; get; } = true;
        public bool SendElectronicInvoice { set; get; } = false;
        public AccountingEntityGraphQLModel AccountingEntity { get; set; }
        public SmtpGraphQLModel Smtp { get; set; }

        public SmtpGraphQLModel NameSmtp { get; set; }
        public AwsSesGraphQLModel AwsSes { get; set; }
        public override string ToString()
        {
            return Name;
        }

        public class EmailDeleteMessage
        {
            public EmailGraphQLModel DeleteEmail { set; get; } = new EmailGraphQLModel();
        }

        public class EmailCreateMessage
        {
            public EmailGraphQLModel CreateEmail { set; get; } = new EmailGraphQLModel();
        }

        public class EmailUpdateMessage
        {
            public EmailGraphQLModel UpdateEmail { set; get; } = new EmailGraphQLModel();
        }         

    }
}
