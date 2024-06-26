using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class MeasurementUnitGrahpQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
    }

    public class MeasurementUnitDTO: MeasurementUnitGrahpQLModel
    {

    }

    public class MeasurementUnitCreateMessage
    {
        public MeasurementUnitGrahpQLModel CreatedMeasurementUnit { get; set; }

        public ObservableCollection<MeasurementUnitGrahpQLModel> MeasurementUnits { get; set; }
    }
    public class MeasurementUnitDeleteMessage
    {
        public MeasurementUnitGrahpQLModel DeletedMeasurementUnit { get; set; }
    }

    public class MeasurementUnitUpdateMessage
    {
        public MeasurementUnitGrahpQLModel UpdatedMeasurementUnit { get; set; }
        public ObservableCollection<MeasurementUnitGrahpQLModel> MeasurementUnits { get; set; }
    }
}
