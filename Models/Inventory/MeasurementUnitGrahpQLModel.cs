using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    public class MeasurementUnitGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DianCode { get; set; } = string.Empty;
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MeasurementUnitDTO : MeasurementUnitGraphQLModel, ICloneable
    {
        public object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class MeasurementUnitCreateMessage
    {

        public required UpsertResponseType<MeasurementUnitGraphQLModel> CreatedMeasurementUnit { get; set; }

    }
    public class MeasurementUnitDeleteMessage
    {
        public required DeleteResponseType DeletedMeasurementUnit { get; set; }
    }

    public class MeasurementUnitUpdateMessage
    {
        public required UpsertResponseType<MeasurementUnitGraphQLModel> UpdatedMeasurementUnit { get; set; } 
    }
}
