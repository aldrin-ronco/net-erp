using Models.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Global
{
    public class CurrencyGraphQLModel
    {
        public int Id { get; set; }
        public LoginAccountGraphQLModel CreatedBy { get; set; } = new();
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MinorUnits { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;

    }
}
