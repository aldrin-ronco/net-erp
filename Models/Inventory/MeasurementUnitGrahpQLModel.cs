using Models.Books;
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

        public UpsertResponseType<MeasurementUnitGraphQLModel> CreatedMeasurementUnit { get; set; } = new();

    }
    public class MeasurementUnitDeleteMessage
    {
        public DeleteResponseType DeletedMeasurementUnit { get; set; } 
    }

    public class MeasurementUnitUpdateMessage
    {
        public UpsertResponseType<MeasurementUnitGraphQLModel> UpdatedMeasurementUnit { get; set; } = new();
    }
}

