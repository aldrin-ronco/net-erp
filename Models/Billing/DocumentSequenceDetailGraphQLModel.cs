using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class DocumentSequenceDetailGraphQLModel
    {
        public int Id { get; set; }
        public int DocumentSequenceMasterId { get; set; }
        public int Number { get; set; }
    }
}
