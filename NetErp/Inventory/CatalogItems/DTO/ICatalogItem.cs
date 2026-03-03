using System.ComponentModel;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public interface ICatalogItem : INotifyPropertyChanged
    {
        int Id { get; set; }
        string Name { get; set; }
    }
}
