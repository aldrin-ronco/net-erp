using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
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
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;
using DevExpress.XtraEditors.Filtering;
using NetErp.Helpers.GraphQLQueryBuilder;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitDetailViewModel : Screen
    {
        public readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;

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
            _ = Task.Run(() => Context.ActivateMasterViewAsync());
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
                    this.TrackChange(nameof(Name));
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
                    this.TrackChange(nameof(Abbreviation));

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
                if (_saveMeasurementUnitCommand is null) _saveMeasurementUnitCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveMeasurementUnitCommand;
            }

        }


        public MeasurementUnitDetailViewModel(MeasurementUnitViewModel context,
            IRepository<MeasurementUnitGraphQLModel> measurementUnitService)
        {
            //_errors = new Dictionary<string, List<string>>();
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _measurementUnitService = measurementUnitService ?? throw new ArgumentNullException(nameof(measurementUnitService));
            Context.EventAggregator.SubscribeOnUIThread(this);
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
            _ = this.SetFocus(nameof(Name));
        }

        public void CleanUpControls()
        {
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            Name = "";
            Abbreviation = "";
        }

        public async Task SaveAsync()
        {
             try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<MeasurementUnitGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new MeasurementUnitCreateMessage() { CreatedMeasurementUnit = result }
                        : new MeasurementUnitUpdateMessage() { UpdatedMeasurementUnit = result }
                );
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

           
        }
        public async Task<UpsertResponseType<MeasurementUnitGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();

                    object variables = new
                    {
                        createResponseInput = new
                        {
                            Name,
                            Abbreviation
                        }
                    };

                    UpsertResponseType<MeasurementUnitGraphQLModel> measurementUnitCreated = await _measurementUnitService.CreateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(query, variables);
                    return measurementUnitCreated;
                }
                else
                {
                    string query = GetUpdateQuery();
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    
                    UpsertResponseType<MeasurementUnitGraphQLModel> updatedMeasurementUnit = await _measurementUnitService.UpdateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(query, variables);
                    return updatedMeasurementUnit;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
          
        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<MeasurementUnitGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "measurementUnit", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Abbreviation)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateMeasurementUnitInput!");

            var fragment = new GraphQLQueryFragment("createMeasurementUnit", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<MeasurementUnitGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "measurementUnit", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Abbreviation)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateMeasurementUnitInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateMeasurementUnit", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public bool CanSave
        {
            get
            {
                // Debe estar el nombre de la unidad de medida
                return    ( (!string.IsNullOrEmpty(Name.Trim()) && !string.IsNullOrEmpty(Abbreviation.Trim())) && this.HasChanges()) ;
             

                
            }
        }
    }
}
