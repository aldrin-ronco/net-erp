using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DTOLibrary.Books;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.DTO.Global;
using Models.Inventory;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Helpers;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitDetailViewModel : Screen
    {
        public readonly IGenericDataAccess<MeasurementUnitGraphQLModel> MeasurementUnitService = IoC.Get<IGenericDataAccess<MeasurementUnitGraphQLModel>>();

        private MeasurementUnitViewModel _context;
        public MeasurementUnitViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new RelayCommand(CanGoBack, GoBack);
                return _goBackCommand;
            }
        }
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }
        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterView());
            CleanUpControls();
        }

        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public bool IsNewRecord
        {
            get { return (this.Id == 0); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (_name is null) return string.Empty;
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _abbreviation = string.Empty;
        public string Abbreviation
        {
            get
            {
                if (_abbreviation is null) return string.Empty;
                return _abbreviation;
            }
            set
            {
                if (_abbreviation != value)
                {
                    _abbreviation = value;
                    NotifyOfPropertyChange(nameof(Abbreviation));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Is Busy
        /// </summary>
        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private ICommand _saveMeasurementUnitCommand;

        public ICommand SaveMeasurementUnitCommand
        {
            get
            {
                if (_saveMeasurementUnitCommand is null) _saveMeasurementUnitCommand = new AsyncCommand(Save, CanSave);
                return _saveMeasurementUnitCommand;
            }

        }


        public MeasurementUnitDetailViewModel(MeasurementUnitViewModel context)
        {
            //_errors = new Dictionary<string, List<string>>();
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = this.SetFocus(nameof(Name));
        }

        public void CleanUpControls()
        {
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            Name = "";
            Abbreviation = "";
        }
        public async Task<IEnumerable<MeasurementUnitGraphQLModel>> LoadMeasurementUnits()
        {
            string queryForPage;
            queryForPage = @"
                query($filter: MeasurementUnitFilterInput){
                    ListResponse: measurementUnits(filter: $filter){
                    id
                    abbreviation
                    name
                    }
                }";
            return await MeasurementUnitService.GetList(queryForPage, new object { });
        }
        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                MeasurementUnitGraphQLModel result = await ExecuteSave();
                var listResult = await LoadMeasurementUnits();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new MeasurementUnitCreateMessage() { CreatedMeasurementUnit = Context.AutoMapper.Map<MeasurementUnitDTO>(result), MeasurementUnits = listResult.ToObservableCollection() });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new MeasurementUnitUpdateMessage() { UpdatedMeasurementUnit = Context.AutoMapper.Map<MeasurementUnitDTO>(result), MeasurementUnits = listResult.ToObservableCollection() });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
        public async Task<MeasurementUnitGraphQLModel> ExecuteSave()
        {
            try
            {
                string query = "";

                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = Name.Trim().RemoveExtraSpaces();
                variables.Data.Abbreviation = Abbreviation.Trim().RemoveExtraSpaces();
                if (!IsNewRecord) variables.Id = Id; // Needed for update only
                if (IsNewRecord)
                {
                    query = @"
                    mutation($data: CreateMeasurementUnitDataInputModelInput!){
                      CreateResponse: createMeasurementUnit(data: $data){
                        id
                        abbreviation
                        name
                      }
                    }";

                    var createdMeasurementUnit = await MeasurementUnitService.Create(query, variables);
                    return createdMeasurementUnit;
                }
                else
                {
                    query = @"
                    mutation($data: UpdateMeasurementUnitDataInputModelInput!, $id: ID){
                      updateMeasurementUnit(data: $data, id: $id){
                        id
                        abbreviation
                        name
                      }
                    }";
                    var updatedMeasurementUnit = await MeasurementUnitService.Update(query, variables);
                    return updatedMeasurementUnit;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public bool CanSave
        {
            get
            {
                // Debe estar el nombre de la unidad de medida
                if (string.IsNullOrEmpty(Name.Trim())) return false;
                // Debe estar la abreviación
                if (string.IsNullOrEmpty(Abbreviation.Trim())) return false;
                return true;
            }
        }
    }
}
