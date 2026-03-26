using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly SmtpCache _smtpCache;

        private readonly IRepository<EmailGraphQLModel> _emailService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Microsoft.VisualStudio.Threading.JoinableTaskFactory _joinableTaskFactory;

        private EmailMasterViewModel _emailMasterViewModel;
        private EmailMasterViewModel EmailMasterViewModel
        {
            get
            {
                if (_emailMasterViewModel is null) 
                    _emailMasterViewModel = new EmailMasterViewModel(this, _emailService, _notificationService);
                return _emailMasterViewModel;
            }
        }
        
        public EmailViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<EmailGraphQLModel> emailService,
            SmtpCache smtpCache,
            Helpers.Services.INotificationService notificationService,
            Microsoft.VisualStudio.Threading.JoinableTaskFactory joinableTaskFactory)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _emailService = emailService;
            _smtpCache = smtpCache;
            _notificationService = notificationService;
            _joinableTaskFactory = joinableTaskFactory;
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterViewAsync();
                }
                catch (Exception ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de email: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(EmailMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                EmailDetailViewModel instance = new(this, _emailService,EventAggregator, _joinableTaskFactory, _smtpCache);
                await instance.InitializeAsync();
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());

            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEditAsync(int id)
        {

            try
            {
                EmailDetailViewModel instance = new(this, _emailService, EventAggregator, _joinableTaskFactory, _smtpCache);
                await instance.InitializeAsync();
                await instance.LoadDataForEditAsync(id);
                instance.SetForEdit();
                instance.AcceptChanges();

               /* instance.Password = email.Password;
                instance.Description = email.Description;
                instance.Email = email.Email;
                instance.Id = email.Id;
                instance.SelectedSmtp = instance.EmailSmtp.FirstOrDefault(smtp => smtp.Id == email.Smtp.Id) ?? throw new Exception();*/ //TODO
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
                
            }
            catch(Exception)
            {
                throw;
            }
        }

    }
}
