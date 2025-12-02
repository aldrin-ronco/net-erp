using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class SmtpGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;

        public override string ToString()
        {
            return Name;
        }

    }

    public class SmtpCreateMessage
    {
        public UpsertResponseType<SmtpGraphQLModel> CreatedSmtp { get; set; } 
    }

    public class SmtpUpdateMessage
    {
        public UpsertResponseType<SmtpGraphQLModel> UpdatedSmtp { get; set; } 
    }

    public class SmtpDeleteMessage
    {
        public DeleteResponseType DeletedSmtp { get; set; } 
    }
}
