using AutoMapper;
using Caliburn.Micro;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using Models.Global;
using NetErp.Global.Smtp.ViewModels;
using Newtonsoft.Json;
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

        private EmailMasterViewModel _emailMasterViewModel;
        private EmailMasterViewModel EmailMasterViewModel
        {
            get
            {

                if (_emailMasterViewModel is null) _emailMasterViewModel = new EmailMasterViewModel(this);
                return _emailMasterViewModel;
            }
        }

        public EmailViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _ = Task.Run(ActivateMasterView);
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

        public async Task ActivateDetailView(EmailGraphQLModel email)
        {
            try
            {
                EmailDetailViewModel instance = new(this);
                instance.EmailSmtp = email.Smtp.Name;
                instance.EmailEmail = email.Email;
                instance.EmailPassword = email.Password;
                instance.EmailDescription = email.Name;

                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
                
            }
            catch(Exception)
            {
                throw;
            }
        }

    }
}
