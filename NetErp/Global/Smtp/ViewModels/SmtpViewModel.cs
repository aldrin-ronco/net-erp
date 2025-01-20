using AutoMapper;
using Caliburn.Micro;
using Models.Global;
using NetErp.Global.CostCenters.ViewModels;
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

        private SmtpMasterViewModel _smtpMasterViewModel;
        public SmtpMasterViewModel SmtpMasterViewModel
        {
            get
            {
                if (_smtpMasterViewModel is null) _smtpMasterViewModel = new SmtpMasterViewModel(this);
                return _smtpMasterViewModel;
            }
        }

        public SmtpViewModel(IMapper mapper,
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
                SmtpDetailViewModel instance = new(this);
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
                SmtpDetailViewModel instance = new(this);
                instance.SmtpId = smtp.Id;
                instance.SmtpName = smtp.Name;
                instance.SmtpHost = smtp.Host;
                instance.SmtpPort = smtp.Port;
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
