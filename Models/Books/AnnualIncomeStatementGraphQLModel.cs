using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AnnualIncomeStatementGraphQLModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal M1 { get; set; } = 0;
        public decimal M2 { get; set; } = 0;
        public decimal M3 { get; set; } = 0;
        public decimal M4 { get; set; } = 0;
        public decimal M5 { get; set; } = 0;
        public decimal M6 { get; set; } = 0;
        public decimal M7 { get; set; } = 0;
        public decimal M8 { get; set; } = 0;
        public decimal M9 { get; set; } = 0;
        public decimal M10 { get; set; } = 0;
        public decimal M11 { get; set; } = 0;
        public decimal M12 { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public decimal Level { get; set; } = 0;
        public string RecordType { get; set; } = string.Empty;

        // String Values

        public string M1StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M1 != 0 ? M1.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M1.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        // String Values

        public string M2StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M2 != 0 ? M2.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M2.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M3StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M3 != 0 ? M3.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M3.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M4StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M4 != 0 ? M4.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M4.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M5StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M5 != 0 ? M5.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M5.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M6StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M6 != 0 ? M6.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M6.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M7StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M7 != 0 ? M7.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M7.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M8StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M8 != 0 ? M8.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M8.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M9StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M9 != 0 ? M9.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M9.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M10StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M10 != 0 ? M10.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M10.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M11StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M11 != 0 ? M11.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M11.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string M12StringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => M12 != 0 ? M12.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => M12.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string TotalStringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => Total != 0 ? Total.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => Total.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        // Negative Check

        public bool IsNegativeM1 => M1 < 0;
        public bool IsNegativeM2 => M2 < 0;
        public bool IsNegativeM3 => M3 < 0;
        public bool IsNegativeM4 => M4 < 0;
        public bool IsNegativeM5 => M5 < 0;
        public bool IsNegativeM6 => M6 < 0;
        public bool IsNegativeM7 => M7 < 0;
        public bool IsNegativeM8 => M8 < 0;
        public bool IsNegativeM9 => M9 < 0;
        public bool IsNegativeM10 => M10 < 0;
        public bool IsNegativeM11 => M11 < 0;
        public bool IsNegativeM12 => M12 < 0;
        public bool IsNegativeTotal => Total < 0;

        // To String
        public override string ToString()
        {
            return $"{Code} - {Name}";
        }

        public class AnnualIncomeStatementDataContext
        {
            public List<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; }
            public List<CostCenterGraphQLModel> CostCenters { get; set; }
        }
    }
}
