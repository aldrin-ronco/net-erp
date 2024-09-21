using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public MeasurementUnitGraphQLModel CreatedMeasurementUnit { get; set; } = new MeasurementUnitGraphQLModel();

        public ObservableCollection<MeasurementUnitGraphQLModel> MeasurementUnits { get; set; } = new ObservableCollection<MeasurementUnitGraphQLModel>();
    }
    public class MeasurementUnitDeleteMessage
    {
        public MeasurementUnitGraphQLModel DeletedMeasurementUnit { get; set; } = new MeasurementUnitGraphQLModel();
    }

    public class MeasurementUnitUpdateMessage
    {
        public MeasurementUnitGraphQLModel UpdatedMeasurementUnit { get; set; } = new MeasurementUnitGraphQLModel();
        public ObservableCollection<MeasurementUnitGraphQLModel> MeasurementUnits { get; set; } = new ObservableCollection<MeasurementUnitGraphQLModel>();
    }
}

