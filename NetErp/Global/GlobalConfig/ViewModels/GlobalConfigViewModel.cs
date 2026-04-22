using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using INotificationService = NetErp.Helpers.Services.INotificationService;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Global.GlobalConfig.ViewModels
{
    public class GlobalConfigViewModel : Screen
    {
        private readonly IRepository<GlobalConfigGraphQLModel> _globalConfigService;
        private readonly AwsS3ConfigCache _awsS3ConfigCache;
        private readonly DianCertificateCache _dianCertificateCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;

        public GlobalConfigViewModel(
            IRepository<GlobalConfigGraphQLModel> globalConfigService,
            AwsS3ConfigCache awsS3ConfigCache,
            DianCertificateCache dianCertificateCache,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory,
            INotificationService notificationService)
        {
            _globalConfigService = globalConfigService;
            _awsS3ConfigCache = awsS3ConfigCache;
            _dianCertificateCache = dianCertificateCache;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;
            _notificationService = notificationService;
        }

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        public bool IsNewRecord => Id == 0;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                NotifyOfPropertyChange(nameof(IsBusy));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AwsS3ConfigGraphQLModel? _selectedDefaultAwsS3Config;

        [ExpandoPath("defaultAwsS3ConfigId", SerializeAsId = true)]
        public AwsS3ConfigGraphQLModel? SelectedDefaultAwsS3Config
        {
            get => _selectedDefaultAwsS3Config;
            set
            {
                if (_selectedDefaultAwsS3Config == value) return;
                _selectedDefaultAwsS3Config = value;
                NotifyOfPropertyChange(nameof(SelectedDefaultAwsS3Config));
                this.TrackChange(nameof(SelectedDefaultAwsS3Config), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private DianCertificateGraphQLModel? _selectedDefaultDianCertificate;

        [ExpandoPath("defaultDianCertificateId", SerializeAsId = true)]
        public DianCertificateGraphQLModel? SelectedDefaultDianCertificate
        {
            get => _selectedDefaultDianCertificate;
            set
            {
                if (_selectedDefaultDianCertificate == value) return;
                _selectedDefaultDianCertificate = value;
                NotifyOfPropertyChange(nameof(SelectedDefaultDianCertificate));
                this.TrackChange(nameof(SelectedDefaultDianCertificate), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public ReadOnlyObservableCollection<AwsS3ConfigGraphQLModel> AwsS3Configs => _awsS3ConfigCache.Items;
        public ReadOnlyObservableCollection<DianCertificateGraphQLModel> DianCertificates => _dianCertificateCache.Items;

        public bool CanSave => this.HasChanges() && !IsBusy;

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;

                await Task.WhenAll(
                    _awsS3ConfigCache.EnsureLoadedAsync(),
                    _dianCertificateCache.EnsureLoadedAsync());

                await LoadGlobalConfigAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(OnInitializedAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }

            await base.OnInitializedAsync(cancellationToken);
        }

        #endregion

        #region Load

        private async Task LoadGlobalConfigAsync()
        {
            var (_, query) = _loadQuery.Value;
            var config = await _globalConfigService.GetSingleItemAsync(query, new { });

            if (config is not null)
            {
                Id = config.Id;

                _selectedDefaultAwsS3Config = AwsS3Configs.FirstOrDefault(x => x.Id == config.DefaultAwsS3Config?.Id);
                NotifyOfPropertyChange(nameof(SelectedDefaultAwsS3Config));

                _selectedDefaultDianCertificate = DianCertificates.FirstOrDefault(x => x.Id == config.DefaultDianCertificate?.Id);
                NotifyOfPropertyChange(nameof(SelectedDefaultDianCertificate));

                SeedCurrentValues();
            }
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedDefaultAwsS3Config), SelectedDefaultAwsS3Config);
            this.SeedValue(nameof(SelectedDefaultDianCertificate), SelectedDefaultDianCertificate);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Save

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                UpsertResponseType<GlobalConfigGraphQLModel> result;

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    result = await _globalConfigService.CreateAsync<UpsertResponseType<GlobalConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    result = await _globalConfigService.UpdateAsync<UpsertResponseType<GlobalConfigGraphQLModel>>(query, variables);
                }

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                if (result.Entity is not null)
                {
                    Id = result.Entity.Id;
                    SessionInfo.DefaultAwsS3Config = result.Entity.DefaultAwsS3Config;
                }

                _notificationService.ShowSuccess(result.Message);
                SeedCurrentValues();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<GlobalConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Select(f => f.DefaultAwsS3Config, nested: aws => aws
                    .Field(a => a.Id)
                    .Field(a => a.AccessKey)
                    .Field(a => a.SecretKey)
                    .Field(a => a.Region)
                    .Field(a => a.Description))
                .Select(f => f.DefaultDianCertificate, nested: dc => dc
                    .Field(d => d.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("globalConfig", [], fields, "SingleItemResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<GlobalConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "globalConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.DefaultAwsS3Config, nested: aws => aws
                        .Field(a => a.Id)
                        .Field(a => a.AccessKey)
                        .Field(a => a.SecretKey)
                        .Field(a => a.Region)
                        .Field(a => a.Description))
                    .Select(f => f.DefaultDianCertificate, nested: dc => dc
                        .Field(d => d.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createGlobalConfig",
                [new("input", "CreateGlobalConfigInput!")],
                fields, "CreateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<GlobalConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "globalConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.DefaultAwsS3Config, nested: aws => aws
                        .Field(a => a.Id)
                        .Field(a => a.AccessKey)
                        .Field(a => a.SecretKey)
                        .Field(a => a.Region)
                        .Field(a => a.Description))
                    .Select(f => f.DefaultDianCertificate, nested: dc => dc
                        .Field(d => d.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateGlobalConfig",
                [new("data", "UpdateGlobalConfigInput!")],
                fields, "UpdateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
