﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class TempRecordGraphQLModel
    {
        public string Id { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int RecordId { get; set; }
    }
}
