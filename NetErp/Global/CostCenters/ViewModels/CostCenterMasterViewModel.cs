using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Controls;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.DB.Helpers;
using Dictionaries;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.CostCenters.PanelEditors;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using Services.Inventory.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.ViewModels
{
    public class CostCenterMasterViewModel : Screen, INotifyDataErrorInfo, 
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterUpdateMessage>,
        IHandle<CostCenterDeleteMessage>,
        IHandle<StorageCreateMessage>,
        IHandle<StorageUpdateMessage>,
        IHandle<StorageDeleteMessage>,
        IHandle<CompanyLocationCreateMessage>,
        IHandle<CompanyLocationUpdateMessage>,
        IHandle<CompanyLocationDeleteMessage>,
        IHandle<CompanyUpdateMessage>
    {
        public CostCenterViewModel Context { get; set; }

        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly IRepository<CountryGraphQLModel> _countryService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;

        Dictionary<string, List<string>> _errors;

        #region Panel Editors

        private CostCenterPanelEditor? _costCenterEditor;
        public CostCenterPanelEditor CostCenterEditor
        {
            get
            {
                if (_costCenterEditor is null)
                    _costCenterEditor = new CostCenterPanelEditor(this, _costCenterService);
                return _costCenterEditor;
            }
        }

        private CompanyPanelEditor? _companyEditor;
        public CompanyPanelEditor CompanyEditor
        {
            get
            {
                if (_companyEditor is null)
                    _companyEditor = new CompanyPanelEditor(this, _companyService, _dialogService);
                return _companyEditor;
            }
        }

        private CompanyLocationPanelEditor? _companyLocationEditor;
        public CompanyLocationPanelEditor CompanyLocationEditor
        {
            get
            {
                if (_companyLocationEditor is null)
                    _companyLocationEditor = new CompanyLocationPanelEditor(this, _companyLocationService);
                return _companyLocationEditor;
            }
        }

        private StoragePanelEditor? _storageEditor;
        public StoragePanelEditor StorageEditor
        {
            get
            {
                if (_storageEditor is null)
                    _storageEditor = new StoragePanelEditor(this, _storageService);
                return _storageEditor;
            }
        }

        #endregion

        #region "TabControls"

        #region "Location"

        private int _companyIdBeforeNewCompanyLocation;

        public int CompanyIdBeforeNewCompanyLocation
        {
            get { return _companyIdBeforeNewCompanyLocation; }
            set
            {
                if(_companyIdBeforeNewCompanyLocation != value)
                {
                    _companyIdBeforeNewCompanyLocation = value;
                    NotifyOfPropertyChange(nameof(CompanyIdBeforeNewCompanyLocation));
                }
            }
        }

        #endregion

        #region "CostCenter"

        private ObservableCollection<CountryGraphQLModel> _countries;

        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get { return _countries; }
            set 
            {
                if (_countries != value)
                {
                    _countries = value;
                    NotifyOfPropertyChange(nameof(Countries));
                }
            }
        }

        public int CompanyLocationIdBeforeNewCostCenter { get; set; }

        public int CompanyLocationIdBeforeNewStorage { get; set; }

        #endregion

        #endregion


        private bool _isNewRecord = false;

        public bool IsNewRecord
        {
            get { return _isNewRecord; }
            set 
            {
                if (_isNewRecord != value) 
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private ICostCentersItems? _selectedItem;

        public ICostCentersItems? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(TabControlVisibility));
                    HandleSelectedItemChanged();
                }
            }
        }

        public void HandleSelectedItemChanged()
        {
            if (_selectedItem != null)
            {
                if (!IsNewRecord)
                {
                    IsEditing = false;
                    CanEdit = true;
                    CanUndo = false;
                    if (_selectedItem is CostCenterDTO costCenterDTO)
                    {
                        CostCenterEditor.SetForEdit(costCenterDTO);
                        return;
                    }
                    if(_selectedItem is StorageDTO storageDTO)
                    {
                        StorageEditor.SetForEdit(storageDTO);
                        return;
                    }
                    if(_selectedItem is CompanyLocationDTO companyLocationDTO)
                    {
                        CompanyLocationEditor.SetForEdit(companyLocationDTO);
                        return;
                    }
                    if(_selectedItem is CompanyDTO companyDTO)
                    {
                        CompanyEditor.SetForEdit(companyDTO);
                        return;
                    }
                }
                else
                {
                    IsEditing = true;
                    CanUndo = true;
                    CanEdit = false;

                    if(_selectedItem is CostCenterDTO costCenterDTO)
                    {
                        CostCenterEditor.SetForNew(CompanyLocationIdBeforeNewCostCenter);
                        return;
                    }
                    if (_selectedItem is StorageDTO storageDTO)
                    {
                        StorageEditor.SetForNew(CompanyLocationIdBeforeNewStorage);
                        return;
                    }
                    if(_selectedItem is CompanyLocationDTO companyLocationDTO)
                    {
                        CompanyLocationEditor.SetForNew(CompanyIdBeforeNewCompanyLocation);
                        return;
                    }
                }
            }
        }

        private int _selectedIndex = 0;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set 
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }

        public bool TabControlVisibility
        {
            get 
            {
                if(_selectedItem != null && _selectedItem is not CostCenterDummyDTO && _selectedItem is not StorageDummyDTO)
                {
                    if (_selectedItem is CompanyDTO companyDTO) CompanyIdBeforeNewCompanyLocation = companyDTO.Id;
                    return true;
                }
                if (_selectedItem is CostCenterDummyDTO costCenterDummyDTO) CompanyLocationIdBeforeNewCostCenter = costCenterDummyDTO.Location.Id;
                if (_selectedItem is StorageDummyDTO storageDummyDTO) CompanyLocationIdBeforeNewStorage = storageDummyDTO.Location.Id;
                SelectedItem = null;
                return false; 
            }
        }

        private ObservableCollection<CompanyDTO> _companies;

        public ObservableCollection<CompanyDTO> Companies
        {
            get { return _companies; }
            set
            {
                if (_companies != value)
                {
                    _companies = value;
                    NotifyOfPropertyChange(nameof(Companies));
                }
            }
        }

        private ObservableCollection<CompanyLocationDTO> _locations;

        public ObservableCollection<CompanyLocationDTO> Locations
        {
            get { return _locations; }
            set
            {
                if (_locations != value)
                {
                    _locations = value;
                    NotifyOfPropertyChange(nameof(Locations));
                }
            }
        }

        private ObservableCollection<CostCenterDTO> _costCenters;

        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get { return _costCenters; }
            set 
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ObservableCollection<StorageDTO> _storages;

        public ObservableCollection<StorageDTO> Storages
        {
            get { return _storages; }
            set 
            {
                if (_storages != value)
                {
                    _storages = value;
                    NotifyOfPropertyChange(nameof(Storages));
                }
            }
        }

        public bool TreeViewIsEnable => !IsEditing;

        private bool _isEditing = false;

        public bool IsEditing
        {
            get { return _isEditing; }
            set 
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(TreeViewIsEnable));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }


        private ICommand _deleteCompanyLocationCommand;
        public ICommand DeleteCompanyLocationCommand
        {
            get
            {
                if (_deleteCompanyLocationCommand is null) _deleteCompanyLocationCommand = new AsyncCommand(DeleteCompanyLocation, CanDeleteCompanyLocation);
                return _deleteCompanyLocationCommand;
            }
        }

        public async Task DeleteCompanyLocation()
        {
            try
            {
                if (SelectedItem is not CompanyLocationDTO companyLocation) return;

                IsBusy = true;
                Refresh();

                int locationId = companyLocation.Id;

                string canDeleteQuery = GetCanDeleteCompanyLocationQuery();
                object canDeleteVariables = new { canDeleteResponseId = locationId };
                CanDeleteType validation = await _companyLocationService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {companyLocation.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado\n\n" + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteCompanyLocationQuery();
                object deleteVariables = new { deleteResponseId = locationId };
                DeleteResponseType deleteResult = await _companyLocationService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro\n\n{deleteResult.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await Context.EventAggregator.PublishOnUIThreadAsync(new CompanyLocationDeleteMessage()
                {
                    DeletedCompanyLocation = deleteResult
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCompanyLocation" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanDeleteCompanyLocation => true;

        private ICommand _deleteStorageCommand;
        public ICommand DeleteStorageCommand
        {
            get
            {
                if (_deleteStorageCommand is null) _deleteStorageCommand = new AsyncCommand(DeleteStorage, CanDeleteStorage);
                return _deleteStorageCommand;
            }
        }

        public async Task DeleteStorage()
        {
            try
            {
                if (SelectedItem is not StorageDTO storage) return;

                IsBusy = true;
                Refresh();

                int storageId = storage.Id;

                string canDeleteQuery = GetCanDeleteStorageQuery();
                object canDeleteVariables = new { canDeleteResponseId = storageId };
                CanDeleteType validation = await _storageService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {storage.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado\n\n" + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteStorageQuery();
                object deleteVariables = new { deleteResponseId = storageId };
                DeleteResponseType deleteResult = await _storageService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro\n\n{deleteResult.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await Context.EventAggregator.PublishOnUIThreadAsync(new StorageDeleteMessage()
                {
                    DeletedStorage = deleteResult
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteStorage" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanDeleteStorage => true;

        private ICommand _deleteCostCenterCommand;
        public ICommand DeleteCostCenterCommand
        {
            get
            {
                if (_deleteCostCenterCommand is null) _deleteCostCenterCommand = new AsyncCommand(DeleteCostCenter, CanDeleteCostCenter);
                return _deleteCostCenterCommand;
            }
        }

        public async Task DeleteCostCenter()
        {
            try
            {
                if (SelectedItem is not CostCenterDTO costCenter) return;

                IsBusy = true;
                Refresh();

                int costCenterId = costCenter.Id;

                string canDeleteQuery = GetCanDeleteCostCenterQuery();
                object canDeleteVariables = new { canDeleteResponseId = costCenterId };
                CanDeleteType validation = await _costCenterService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {costCenter.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado\n\n" + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteCostCenterQuery();
                object deleteVariables = new { deleteResponseId = costCenterId };
                DeleteResponseType deleteResult = await _costCenterService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro\n\n{deleteResult.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await Context.EventAggregator.PublishOnUIThreadAsync(new CostCenterDeleteMessage()
                {
                    DeletedCostCenter = deleteResult
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCostCenter" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanDeleteCostCenter()
        {
            return true;
        }

        private ICommand _createCompanyLocationCommand;
        public ICommand CreateCompanyLocationCommand
        {
            get
            {
                if (_createCompanyLocationCommand is null) _createCompanyLocationCommand = new AsyncCommand(CreateCompanyLocation, CanCreateCompanyLocation);
                return _createCompanyLocationCommand;
            }
        }

        public async Task CreateCompanyLocation()
        {
            IsNewRecord = true;
            SelectedItem = new CompanyLocationDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("CompanyLocationName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateCompanyLocation => true;

        private ICommand _createStorageCommand;
        public ICommand CreateStorageCommand
        {
            get
            {
                if (_createStorageCommand is null) _createStorageCommand = new AsyncCommand(CreateStorage, CanCreateStorage);
                return _createStorageCommand;
            }
        }
        public async Task CreateStorage()
        {
            IsNewRecord = true;
            SelectedItem = new StorageDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("StorageName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateStorage => true;

        private ICommand _createCostCenterCommand;
        public ICommand CreateCostCenterCommand
        {
            get
            {
                if (_createCostCenterCommand is null) _createCostCenterCommand = new AsyncCommand(CreateCostCenter, CanCreateCostCenter);
                return _createCostCenterCommand;
            }
        }

        public async Task CreateCostCenter()
        {
            IsNewRecord = true;
            SelectedItem = new CostCenterDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("CostCenterName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateCostCenter()
        {
            return true;
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
                return _saveCommand;
            }
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();

                bool success = false;

                if (SelectedItem is CostCenterDTO)
                {
                    success = await CostCenterEditor.SaveAsync();
                }
                else if (SelectedItem is StorageDTO)
                {
                    success = await StorageEditor.SaveAsync();
                }
                else if (SelectedItem is CompanyLocationDTO)
                {
                    success = await CompanyLocationEditor.SaveAsync();
                }
                else if (SelectedItem is CompanyDTO)
                {
                    success = await CompanyEditor.SaveAsync();
                }

                // Solo resetear estado si el guardado fue exitoso
                if (success)
                {
                    IsEditing = false;
                    CanUndo = false;
                    CanEdit = true;
                    SelectedIndex = 0;
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanSave
        {
            get
            {
                if (SelectedItem is CostCenterDTO)
                    return CostCenterEditor.CanSave;

                if (SelectedItem is CompanyDTO)
                    return CompanyEditor.CanSave;

                if (SelectedItem is CompanyLocationDTO)
                    return CompanyLocationEditor.CanSave;

                if (SelectedItem is StorageDTO)
                    return StorageEditor.CanSave;

                return false;
            }
        }

        public void RefreshCanSave() => NotifyOfPropertyChange(nameof(CanSave));

        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand is null) _editCommand = new AsyncCommand(Edit, CanEdit);
                return _editCommand;
            }
        }

        public async Task Edit()
        {
            // Actualizar estado del Ribbon (pertenece a MasterViewModel)
            CanEdit = false;
            CanUndo = true;
            IsEditing = true;

            if (SelectedItem is CostCenterDTO)
            {
                CostCenterEditor.IsEditing = true;
                this.SetFocus("CostCenterName");
                return;
            }

            if (SelectedItem is CompanyDTO)
            {
                CompanyEditor.IsEditing = true;
                return;
            }

            if (SelectedItem is CompanyLocationDTO)
            {
                CompanyLocationEditor.IsEditing = true;
                this.SetFocus("CompanyLocationName");
                return;
            }

            if (SelectedItem is StorageDTO)
            {
                StorageEditor.IsEditing = true;
                this.SetFocus("StorageName");
                return;
            }
        }

        private bool _canEdit = true;

        public bool CanEdit
        {
            get { return _canEdit; }
            set 
            {
                if (_canEdit != value)
                {
                    _canEdit = value;
                    NotifyOfPropertyChange(nameof(CanEdit));
                }
            }
        }

        private ICommand _undoCommand;

        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand is null) _undoCommand = new DelegateCommand(Undo, CanUndo);
                return _undoCommand;
            }
        }

        public void Undo()
        {
            // Restaurar estado del Ribbon y del MasterViewModel
            CanUndo = false;
            CanEdit = true;
            IsEditing = false;

            // IMPORTANTE: Poner IsNewRecord = false ANTES de llamar al Undo del PanelEditor
            // porque el Undo puede modificar SelectedItem y el setter verifica IsNewRecord
            IsNewRecord = false;

            if (SelectedItem is CostCenterDTO)
            {
                CostCenterEditor.Undo();
                SelectedIndex = 0;
                return;
            }

            if (SelectedItem is CompanyDTO)
            {
                CompanyEditor.Undo();
                SelectedIndex = 0;
                return;
            }

            if (SelectedItem is CompanyLocationDTO)
            {
                CompanyLocationEditor.Undo();
                SelectedIndex = 0;
                return;
            }

            if (SelectedItem is StorageDTO)
            {
                StorageEditor.Undo();
                SelectedIndex = 0;
                return;
            }
        }

        private bool _canUndo = false;
        public bool CanUndo
        {
            get { return _canUndo; }
            set
            {
                if (_canUndo != value)
                {
                    _canUndo = value;
                    NotifyOfPropertyChange(nameof(CanUndo));
                }
            }
        }


        public async Task LoadStoragesAsync(CompanyLocationDTO location, StorageDummyDTO storageDummyDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    storageDummyDTO.Storages.Remove(storageDummyDTO.Storages[0]);
                });

                string query = GetLoadStoragesQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;
                variables.pageResponseCompanyLocationId = location.Id;

                PageType<StorageGraphQLModel> result = await _storageService.GetPageAsync(query, variables);
                Storages = Context.AutoMapper.Map<ObservableCollection<StorageDTO>>(result.Entries);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (StorageDTO storage in Storages)
                    {
                        storageDummyDTO.Storages.Add(storage);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadStorages" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadCostCentersAsync(CompanyLocationDTO location, CostCenterDummyDTO costCenterDummyDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    costCenterDummyDTO.CostCenters.Remove(costCenterDummyDTO.CostCenters[0]);
                });

                string query = GetLoadCostCentersQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.companyLocationId = location.Id;
                variables.pageResponsePagination.pageSize = -1;

                PageType<CostCenterGraphQLModel> result = await _costCenterService.GetPageAsync(query, variables);
                CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(result.Entries);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (CostCenterDTO costCenter in CostCenters)
                    {
                        costCenterDummyDTO.CostCenters.Add(costCenter);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public async Task LoadCompaniesLocationsAsync(CompanyDTO company)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    company.Locations.Remove(company.Locations[0]);
                });

                string query = GetLoadCompaniesLocationsQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;

                PageType<CompanyLocationGraphQLModel> source = await _companyLocationService.GetPageAsync(query, variables);
                Locations = Context.AutoMapper.Map<ObservableCollection<CompanyLocationDTO>>(source.Entries);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (CompanyLocationDTO location in Locations)
                    {
                        location.Context = this;
                        location.DummyItems.Add(new CostCenterDummyDTO(this, location));
                        location.DummyItems.Add(new StorageDummyDTO(this, location));
                        company.Locations.Add(location);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompaniesLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        private string GetLoadCompaniesLocationsQuery()
        {
            var fields = FieldSpec<PageType<CompanyLocationGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.Company, company => company
                        .Field(c => c.Id)))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("companyLocationsPage", [parameter], fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadStoragesQuery()
        {
            var fields = FieldSpec<PageType<StorageGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Address)
                    .Field(e => e.State)
                    .Select(e => e.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Select(c => c.Department, dept => dept
                            .Field(d => d.Id)
                            .Field(d => d.Code)
                            .Field(d => d.Name)
                            .Select(d => d.Country, country => country
                                .Field(co => co.Id)
                                .Field(co => co.Code)
                                .Field(co => co.Name))))
                    .Select(e => e.Location, loc => loc
                        .Field(l => l.Id)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("companyLocationId", "ID")
            };
            var fragment = new GraphQLQueryFragment("storagesPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadCostCentersQuery()
        {
            var fields = FieldSpec<PageType<CostCenterGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.TradeName)
                    .Field(e => e.Status)
                    .Field(e => e.ShortName)
                    .Field(e => e.Address)
                    .Field(e => e.PrimaryPhone)
                    .Field(e => e.SecondaryPhone)
                    .Field(e => e.PrimaryCellPhone)
                    .Field(e => e.SecondaryCellPhone)
                    .Field(e => e.DateControlType)
                    .Field(e => e.ShowChangeWindowOnCash)
                    .Field(e => e.AllowBuy)
                    .Field(e => e.AllowSell)
                    .Field(e => e.IsTaxable)
                    .Field(e => e.PriceListIncludeTax)
                    .Field(e => e.InvoicePriceIncludeTax)
                    .Field(e => e.InvoiceCopiesToPrint)
                    .Field(e => e.RequiresConfirmationToPrintCopies)
                    .Field(e => e.AllowRepeatItemsOnSales)
                    .Field(e => e.TaxToCost)
                    .Select(e => e.CompanyLocation, loc => loc
                        .Field(l => l.Id))
                    .Select(e => e.Country, country => country
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(e => e.Department, dept => dept
                        .Field(d => d.Id)
                        .Field(d => d.Code)
                        .Field(d => d.Name))
                    .Select(e => e.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "CostCenterFilters")
            };
            var fragment = new GraphQLQueryFragment("costCentersPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #region Delete Queries

        private string GetCanDeleteCompanyLocationQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteCompanyLocation", [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        private string GetDeleteCompanyLocationQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteCompanyLocation", [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        private string GetCanDeleteStorageQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteStorage", [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        private string GetDeleteStorageQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteStorage", [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        private string GetCanDeleteCostCenterQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteCostCenter", [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        private string GetDeleteCostCenterQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteCostCenter", [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        #endregion

        public async Task InitializeAsync()
        {
            await LoadCompanyAsync();
            LoadComboBoxes();
        }

        public void LoadComboBoxes()
        {
            Countries = GlobalDataCache.Countries;
        }

        public async Task LoadCompanyAsync()
        {
            try
            {
                Refresh();
                string query = GetLoadCompanyQuery();

                dynamic variables = new ExpandoObject();
                variables.singleItemResponseId = SessionInfo.CurrentCompany!.Id; //A este punto la empresa sí o sí tuvo que ser seleccionada

                CompanyGraphQLModel company = await _companyService.FindByIdAsync(query, variables);
                ObservableCollection<CompanyGraphQLModel> source = [company]; // Paso necesario porque se recibe un solo elemento y el mapeo se hace a una colección
                Companies = Context.AutoMapper.Map<ObservableCollection<CompanyDTO>>(source);
                if (Companies.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (CompanyDTO company in Companies)
                        {
                            company.Context = this;
                            company.Locations.Add(new CompanyLocationDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
                        }
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompany" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        private string GetLoadCompanyQuery()
        {
            var fields = FieldSpec<CompanyGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Select(f => f.CompanyEntity, nested => nested
                    .Field(n => n.SearchName))
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("company", [parameter], fields, "SingleItemResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }


        public CostCenterMasterViewModel(
            CostCenterViewModel context,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IRepository<CostCenterGraphQLModel> costCenterService,
            IRepository<StorageGraphQLModel> storageService,
            IRepository<CountryGraphQLModel> countryService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService) 
        {
            Context = context;
            _companyService = companyService;
            _companyLocationService = companyLocationService;
            _costCenterService = costCenterService;
            _storageService = storageService;
            _countryService = countryService;
            _dialogService = dialogService;
            _notificationService = notificationService;

            _errors = new Dictionary<string, List<string>>();
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await InitializeAsync();
        }

        public async Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            CostCenterDTO costCenterDTO = Context.AutoMapper.Map<CostCenterDTO>(message.CreatedCostCenter.Entity);
            CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == costCenterDTO.CompanyLocation.Company.Id) ?? throw new Exception("");
            if (companyDTO == null) return;
            CompanyLocationDTO companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == costCenterDTO.CompanyLocation.Id) ?? throw new Exception("");
            if (companyLocationDTO == null) return;
            CostCenterDummyDTO costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is CostCenterDummyDTO) as CostCenterDummyDTO ?? throw new Exception("");
            if (costCenterDummyDTO == null) return;
            if(!costCenterDummyDTO.IsExpanded && costCenterDummyDTO.CostCenters[0].IsDummyChild)
            {
                await LoadCostCentersAsync(companyLocationDTO, costCenterDummyDTO);
                costCenterDummyDTO.IsExpanded = true;
                CostCenterDTO? costCenter = costCenterDummyDTO.CostCenters.FirstOrDefault(x => x.Id == costCenterDTO.Id);
                if (costCenter is null) return;
                _notificationService.ShowSuccess(message.CreatedCostCenter.Message);
                SelectedItem = costCenter;
                return;
            }
            if (!costCenterDummyDTO.IsExpanded)
            {
                costCenterDummyDTO.IsExpanded = true;
                costCenterDummyDTO.CostCenters.Add(costCenterDTO);
                SelectedItem = costCenterDTO;
                _notificationService.ShowSuccess(message.CreatedCostCenter.Message);
                return;
            }
            costCenterDummyDTO.CostCenters.Add(costCenterDTO);
            SelectedItem = costCenterDTO;
            _notificationService.ShowSuccess(message.CreatedCostCenter.Message);
            return;
        }

        public Task HandleAsync(CostCenterUpdateMessage message, CancellationToken cancellationToken)
        {
            CostCenterDTO costCenterDTO = Context.AutoMapper.Map<CostCenterDTO>(message.UpdatedCostCenter.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(company => company.Id == costCenterDTO.CompanyLocation.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == costCenterDTO.CompanyLocation.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            CostCenterDummyDTO? costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is CostCenterDummyDTO) as CostCenterDummyDTO;
            if (costCenterDummyDTO is null) return Task.CompletedTask;
            CostCenterDTO? costCenterToUpdate = costCenterDummyDTO.CostCenters.FirstOrDefault(costCenter => costCenter.Id == costCenterDTO.Id);
            if (costCenterToUpdate is null) return Task.CompletedTask;
            costCenterToUpdate.Id = costCenterDTO.Id;
            costCenterToUpdate.Name = costCenterDTO.Name;
            costCenterToUpdate.TradeName = costCenterDTO.TradeName;
            costCenterToUpdate.ShortName = costCenterDTO.ShortName;
            costCenterToUpdate.Status = costCenterDTO.Status;
            costCenterToUpdate.Address = costCenterDTO.Address;
            costCenterToUpdate.PrimaryPhone = costCenterDTO.PrimaryPhone;
            costCenterToUpdate.SecondaryPhone = costCenterDTO.SecondaryPhone;
            costCenterToUpdate.PrimaryCellPhone = costCenterDTO.PrimaryCellPhone;
            costCenterToUpdate.SecondaryCellPhone = costCenterDTO.SecondaryCellPhone;
            costCenterToUpdate.DateControlType = costCenterDTO.DateControlType;
            costCenterToUpdate.ShowChangeWindowOnCash = costCenterDTO.ShowChangeWindowOnCash;
            costCenterToUpdate.AllowBuy = costCenterDTO.AllowBuy;
            costCenterToUpdate.AllowSell = costCenterDTO.AllowSell;
            costCenterToUpdate.IsTaxable = costCenterDTO.IsTaxable;
            costCenterToUpdate.PriceListIncludeTax = costCenterDTO.PriceListIncludeTax;
            costCenterToUpdate.InvoicePriceIncludeTax = costCenterDTO.InvoicePriceIncludeTax;
            costCenterToUpdate.AllowRepeatItemsOnSales = costCenterDTO.AllowRepeatItemsOnSales;
            costCenterToUpdate.InvoiceCopiesToPrint = costCenterDTO.InvoiceCopiesToPrint;
            costCenterToUpdate.RequiresConfirmationToPrintCopies = costCenterDTO.RequiresConfirmationToPrintCopies;
            costCenterToUpdate.TaxToCost = costCenterDTO.TaxToCost;
            costCenterToUpdate.Country = costCenterDTO.Country;
            costCenterToUpdate.Department = costCenterDTO.Department;
            costCenterToUpdate.City = costCenterDTO.City;
            costCenterToUpdate.CompanyLocation = costCenterDTO.CompanyLocation;
            _notificationService.ShowSuccess(message.UpdatedCostCenter.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var company in Companies)
                {
                    foreach (var location in company.Locations)
                    {
                        var costCenterDummy = location.DummyItems.FirstOrDefault(dummy => dummy is CostCenterDummyDTO) as CostCenterDummyDTO;
                        if (costCenterDummy == null) continue;

                        var costCenterToRemove = costCenterDummy.CostCenters.FirstOrDefault(cc => cc.Id == message.DeletedCostCenter.DeletedId);
                        if (costCenterToRemove != null)
                        {
                            costCenterDummy.CostCenters.Remove(costCenterToRemove);
                            return;
                        }
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedCostCenter.Message);
            return Task.CompletedTask;
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearAllErrors()
        {
            _errors.Clear();
        }

        public async Task HandleAsync(StorageCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            StorageDTO storageDTO = Context.AutoMapper.Map<StorageDTO>(message.CreatedStorage.Entity);
            CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == storageDTO.Location.Company.Id) ?? throw new Exception("");
            if (companyDTO == null) return;
            CompanyLocationDTO companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == storageDTO.Location.Id) ?? throw new Exception("");
            if (companyLocationDTO == null) return;
            StorageDummyDTO storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is StorageDummyDTO) as StorageDummyDTO ?? throw new Exception("");
            if (storageDummyDTO == null) return;
            if (!storageDummyDTO.IsExpanded && storageDummyDTO.Storages[0].IsDummyChild)
            {
                await LoadStoragesAsync(companyLocationDTO, storageDummyDTO);
                storageDummyDTO.IsExpanded = true;
                StorageDTO? storage = storageDummyDTO.Storages.FirstOrDefault(x => x.Id == storageDTO.Id);
                if (storage is null) return;
                SelectedItem = storage;
                _notificationService.ShowSuccess(message.CreatedStorage.Message);
                return;
            }
            if (!storageDummyDTO.IsExpanded)
            {
                storageDummyDTO.IsExpanded = true;
                storageDummyDTO.Storages.Add(storageDTO);
                SelectedItem = storageDTO;
                _notificationService.ShowSuccess(message.CreatedStorage.Message);
                return;
            }
            storageDummyDTO.Storages.Add(storageDTO);
            SelectedItem = storageDTO;
            _notificationService.ShowSuccess(message.CreatedStorage.Message);
            return;
        }

        public Task HandleAsync(StorageUpdateMessage message, CancellationToken cancellationToken)
        {
            StorageDTO storageDTO = Context.AutoMapper.Map<StorageDTO>(message.UpdatedStorage.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(company => company.Id == storageDTO.Location.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == storageDTO.Location.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            StorageDummyDTO? storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is StorageDummyDTO) as StorageDummyDTO;
            if (storageDummyDTO is null) return Task.CompletedTask;
            StorageDTO? storageToUpdate = storageDummyDTO.Storages.FirstOrDefault(costCenter => costCenter.Id == storageDTO.Id);
            if (storageToUpdate is null) return Task.CompletedTask;
            storageToUpdate.Id = storageDTO.Id;
            storageToUpdate.Name = storageDTO.Name;
            storageToUpdate.Address = storageDTO.Address;
            storageToUpdate.State = storageDTO.State;
            storageToUpdate.City = storageDTO.City;
            storageToUpdate.Location = storageDTO.Location;
            _notificationService.ShowSuccess(message.UpdatedStorage.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(StorageDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var company in Companies)
                {
                    foreach (var location in company.Locations)
                    {
                        var storageDummy = location.DummyItems.FirstOrDefault(dummy => dummy is StorageDummyDTO) as StorageDummyDTO;
                        if (storageDummy == null) continue;

                        var storageToRemove = storageDummy.Storages.FirstOrDefault(s => s.Id == message.DeletedStorage.DeletedId);
                        if (storageToRemove != null)
                        {
                            storageDummy.Storages.Remove(storageToRemove);
                            return;
                        }
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedStorage.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(CompanyLocationCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            CompanyLocationDTO companyLocationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(message.CreatedCompanyLocation.Entity);
            CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == companyLocationDTO.Company.Id) ?? throw new Exception("");
            if (companyDTO is null) return;
            if (!companyDTO.IsExpanded && companyDTO.Locations[0].IsDummyChild)
            {
                await LoadCompaniesLocationsAsync(companyDTO);
                companyDTO.IsExpanded = true;
                CompanyLocationDTO? companyLocation = companyDTO.Locations.FirstOrDefault(x => x.Id == companyLocationDTO.Id);
                if (companyLocation is null) return;
                _notificationService.ShowSuccess(message.CreatedCompanyLocation.Message);
                SelectedItem = companyLocation;
                return;
            }
            if (!companyDTO.IsExpanded)
            {
                companyDTO.IsExpanded = true;
                companyLocationDTO.DummyItems.Add(new CostCenterDummyDTO(this, companyLocationDTO));
                companyLocationDTO.DummyItems.Add(new StorageDummyDTO(this, companyLocationDTO));
                companyDTO.Locations.Add(companyLocationDTO);
                SelectedItem = companyLocationDTO;
                _notificationService.ShowSuccess(message.CreatedCompanyLocation.Message);
                return;
            }
            companyLocationDTO.DummyItems.Add(new CostCenterDummyDTO(this, companyLocationDTO));
            companyLocationDTO.DummyItems.Add(new StorageDummyDTO(this, companyLocationDTO));
            companyDTO.Locations.Add(companyLocationDTO);
            SelectedItem = companyLocationDTO;
            _notificationService.ShowSuccess(message.CreatedCompanyLocation.Message);
            return;
        }

        public Task HandleAsync(CompanyLocationUpdateMessage message, CancellationToken cancellationToken)
        {
            CompanyLocationDTO companyLocationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(message.UpdatedCompanyLocation.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(company => company.Id == companyLocationDTO.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationToUpdate = companyDTO.Locations.FirstOrDefault(location => location.Id == companyLocationDTO.Id);
            if (companyLocationToUpdate is null) return Task.CompletedTask;
            companyLocationToUpdate.Id = companyLocationDTO.Id;
            companyLocationToUpdate.Name = companyLocationDTO.Name;
            companyLocationToUpdate.Company = companyLocationDTO.Company;
            _notificationService.ShowSuccess(message.UpdatedCompanyLocation.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyLocationDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var company in Companies)
                {
                    var locationToRemove = company.Locations.FirstOrDefault(loc => loc.Id == message.DeletedCompanyLocation.DeletedId);
                    if (locationToRemove != null)
                    {
                        company.Locations.Remove(locationToRemove);
                        return;
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedCompanyLocation.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyUpdateMessage message, CancellationToken cancellationToken)
        {
            CompanyDTO companyDTO = Context.AutoMapper.Map<CompanyDTO>(message.UpdatedCompany.Entity);
            CompanyDTO? companyToUpdate = Companies.FirstOrDefault(company => company.Id == companyDTO.Id);
            if (companyToUpdate is null) return Task.CompletedTask;
            companyToUpdate.Id = companyDTO.Id;
            companyToUpdate.CompanyEntity = companyDTO.CompanyEntity;
            _notificationService.ShowSuccess(message.UpdatedCompany.Message);
            return Task.CompletedTask;
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
