using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Global;
using NetErp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        
        private readonly IRepository<SmtpGraphQLModel> _smtpService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private SmtpMasterViewModel _smtpMasterViewModel;
        public SmtpMasterViewModel SmtpMasterViewModel
        {
            get
            {
                if (_smtpMasterViewModel is null) 
                    _smtpMasterViewModel = new SmtpMasterViewModel(this, _smtpService, _notificationService);
                return _smtpMasterViewModel;
            }
        }

        public SmtpViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<SmtpGraphQLModel> smtpService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _smtpService = smtpService;
            _notificationService = notificationService;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterView();
                }
                catch (Exception ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de SMTP: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(SmtpMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForNew()
        {
            try
            {
                SmtpDetailViewModel instance = new(this, _smtpService);
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForEdit(SmtpGraphQLModel smtp)
        {
            try
            {
                SmtpDetailViewModel instance = new(this, _smtpService);
                instance.SmtpId = smtp.Id;
                instance.Name = smtp.Name;
                instance.Host = smtp.Host;
                instance.Port = smtp.Port;
                instance.AcceptChanges();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
