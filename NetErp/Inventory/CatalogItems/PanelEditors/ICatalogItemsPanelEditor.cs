using System.ComponentModel;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.PanelEditors
{
    public interface ICatalogItemsPanelEditor : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        bool IsEditing { get; set; }
        bool IsNewRecord { get; }
        bool CanSave { get; }
        bool IsBusy { get; set; }
        void SetForNew(object context);
        void SetForEdit(object dto);
        Task<bool> SaveAsync();
        void Undo();
        void ValidateAll();
        void ClearAllErrors();
    }
}
