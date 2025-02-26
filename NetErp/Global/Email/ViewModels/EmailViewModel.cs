using AutoMapper;
using Caliburn.Micro;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Grid;
using Models.Global;
using NetErp.Global.Smtp.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.Primitives;

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
        public async Task ActivateDetailViewForNew()
        {
            try
            {
                EmailDetailViewModel instance = new(this);
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
                EmailDetailViewModel instance = new(this);
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
