using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Global;
using NetErp.Helpers;
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
        
        private readonly IRepository<EmailGraphQLModel> _emailService;
        private readonly IRepository<SmtpGraphQLModel> _smtpService;
        private readonly Helpers.Services.INotificationService _notificationService;
        
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
            IRepository<SmtpGraphQLModel> smtpService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _emailService = emailService;
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
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de email: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterView()
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
        public async Task ActivateDetailViewForNew()
        {
            try
            {
                EmailDetailViewModel instance = new(this, _emailService, _smtpService);
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());

            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEdit(EmailGraphQLModel email)
        {

            try
            {
                EmailDetailViewModel instance = new(this, _emailService, _smtpService);
                instance.EmailPassword = email.Password;
                instance.EmailDescription = email.Description;
                instance.EmailEmail = email.Email;
                instance.EmailId = email.Id;
                instance.SelectedSmtp = instance.EmailSmtp.FirstOrDefault(smtp => smtp.Id == email.Smtp.Id) ?? throw new Exception(); //TODO
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
                
            }
            catch(Exception)
            {
                throw;
            }
        }

    }
}
