using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.MenuItem.ViewModels
{
    public class MenuItemOrderViewModel : Screen
    {
        #region Dependencies

        private readonly IRepository<MenuItemGraphQLModel> _menuItemService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Dialog Size

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 400;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 400;

        #endregion

        #region Properties

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public bool OrderChanged { get; private set; }

        private readonly List<int> _originalOrder;

        public ObservableCollection<MenuItemGraphQLModel> Items
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Items));
                }
            }
        } = [];

        public bool CanSave => HasOrderChanged();

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

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Constructor

        public MenuItemOrderViewModel(
            IRepository<MenuItemGraphQLModel> menuItemService,
            JoinableTaskFactory joinableTaskFactory,
            List<MenuItemGraphQLModel> items)
        {
            _menuItemService = menuItemService ?? throw new ArgumentNullException(nameof(menuItemService));
            _joinableTaskFactory = joinableTaskFactory;
            Items = new ObservableCollection<MenuItemGraphQLModel>(items);
            Items.CollectionChanged += (_, _) => NotifyOfPropertyChange(nameof(CanSave));
            _originalOrder = [.. items.Select(i => i.Id)];
        }

        #endregion

        #region Methods

        private bool HasOrderChanged()
        {
            List<int> currentOrder = Items.Select(i => i.Id).ToList();
            return !_originalOrder.SequenceEqual(currentOrder);
        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                List<object> updates = [];
                for (int i = 0; i < Items.Count; i++)
                {
                    updates.Add(new { id = Items[i].Id, displayOrder = i + 1 });
                }

                (GraphQLQueryFragment fragment, string query) = _bulkUpdateOrderQuery.Value;
                dynamic variables = new GraphQLVariables()
                    .For(fragment, "input", updates)
                    .Build();

                await _menuItemService.MutationContextAsync<object>(query, variables);
                OrderChanged = true;
                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _bulkUpdateOrderQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<MenuItemGraphQLModel>>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("bulkUpdateMenuItemsDisplayOrder",
                [new("input", "[MenuItemDisplayOrderUpdateInput!]!")],
                fields, "BulkUpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
