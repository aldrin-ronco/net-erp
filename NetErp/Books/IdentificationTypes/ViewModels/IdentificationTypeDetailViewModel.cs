using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers;
using Extensions.Books;
using System;
using System.Threading.Tasks;
using Common.Helpers;
using DevExpress.Mvvm;
using System.Windows.Input;
using Dictionaries;
using Models.DTO.Global;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Common.Extensions;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using System.Windows.Threading;

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeDetailViewModel : Screen
    {
        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        #region Propiedades
        // Context
        private IdentificationTypeViewModel _context;
        public IdentificationTypeViewModel Context
        {
            get => _context;
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        // Is Busy
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

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
                return _saveCommand;
            }
        }

        // Si es un nuevo registro
        public bool IsNewRecord => Id == 0;

        // Model IdentificationType 
        private IdentificationTypeGraphQLModel _identificationType;
        public IdentificationTypeGraphQLModel IdentificationType
        {
            get => _identificationType;
            set
            {
                if (_identificationType != value)
                {
                    _identificationType = value;
                    NotifyOfPropertyChange(nameof(IdentificationType));
                }
            }
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
                }
            }
        }


        private string _code;

        public string Code
        {
            get { return _code; }
            set 
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
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

        private bool _hasVerificationDigit;

        public bool HasVerificationDigit
        {
            get { return _hasVerificationDigit; }
            set 
            {
                if (_hasVerificationDigit != value)
                {
                    _hasVerificationDigit = value;
                    NotifyOfPropertyChange(nameof(HasVerificationDigit));
                }
            }
        }

        private int _minimumDocumentLength;

        public int MinimumDocumentLength
        {
            get { return _minimumDocumentLength; }
            set 
            {
                if (_minimumDocumentLength != value)
                {
                    _minimumDocumentLength = value;
                    NotifyOfPropertyChange(nameof(MinimumDocumentLength));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }


        #endregion

        public IdentificationTypeDetailViewModel(IdentificationTypeViewModel context, IRepository<IdentificationTypeGraphQLModel> identificationTypeService)
        {
            this._identificationTypeService = identificationTypeService;
            Context = context;
            IdentificationType = new IdentificationTypeGraphQLModel();
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            _ = App.Current.Dispatcher.BeginInvoke(() =>
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(Code))), DispatcherPriority.Render);
            });
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewAsync());
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                IdentificationTypeGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new IdentificationTypeCreateMessage() { CreatedIdentificationType = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new IdentificationTypeUpdateMessage() { UpdatedIdentificationType = result });
                }
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

        public async Task<IdentificationTypeGraphQLModel> ExecuteSaveAsync()
        {

            try
            {
                if (IsNewRecord)
                {
                    string query = @"
				mutation ($data: CreateIdentificationTypeInput!) {
				  CreateResponse: createIdentificationType(data: $data) {
				    id
				    code
				    name
				    hasVerificationDigit
				    minimumDocumentLength
				  }
				}";

                    object variables = new
                    {
                        Data = new
                        {
                            Code,
                            Name,
                            HasVerificationDigit,
                            MinimumDocumentLength
                        }
                    };

                    var identificationTypeCreated = await _identificationTypeService.CreateAsync(query, variables);
                    return identificationTypeCreated;
                }
                else
                {
                    string query = @"
					mutation ($data: UpdateIdentificationTypeInput!, $id: Int!) {
					  UpdateResponse: updateIdentificationType(data: $data, id: $id) {
						id
						code
						name
						hasVerificationDigit
						minimumDocumentLength
					  }
					}";

                    object variables = new
                    {
                        Data = new
                        {
                            Code,
                            Name,
                            HasVerificationDigit,
                            MinimumDocumentLength
                        },
                        Id
                    };

                    IdentificationTypeGraphQLModel updatedIdentificationType = await _identificationTypeService.UpdateAsync(query, variables);
                    return updatedIdentificationType;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void CleanUpControlsForNew()
        {
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            Code = string.Empty;
            Name = string.Empty;
            HasVerificationDigit = false;
            MinimumDocumentLength = 7;
        }

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Code) || Code.Length != 2) return false;
                if (string.IsNullOrEmpty(Name)) return false;
                if (MinimumDocumentLength == 0) return false;
                return true;
            }
        }
    }
}
