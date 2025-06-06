﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public SmtpGraphQLModel CreatedSmtp { get; set; } = new SmtpGraphQLModel();
    }

    public class SmtpUpdateMessage
    {
        public SmtpGraphQLModel UpdatedSmtp { get; set; } = new SmtpGraphQLModel();
    }

    public class SmtpDeleteMessage
    {
        public SmtpGraphQLModel DeletedSmtp { get; set; } = new SmtpGraphQLModel();
    }
}
