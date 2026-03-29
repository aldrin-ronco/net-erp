using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class SystemAccountGraphQLModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FirstLastName { get; set; } = string.Empty;
        public string MiddleLastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhotoUrl {  get; set; } = string.Empty;
        public string Profession {  get; set; } = string.Empty;
        public bool IsSystemAdmin { get; set; }
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
