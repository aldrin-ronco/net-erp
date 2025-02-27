using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm;
using Models.Billing;
using Models.Global;
using Ninject.Activation;
using Services.Billing.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneDetailViewModel : Screen
    {
        public IGenericDataAccess<ZoneGraphQLModel> ZoneService = IoC.Get<IGenericDataAccess<ZoneGraphQLModel>>();
        public ZoneViewModel Context { get; set; }
        
        public ZoneDetailViewModel(ZoneViewModel context) 
        {
            Context = context;
        }
        private bool _CanSave;
        public bool CanSave
        {
            get { return _CanSave; }
            set
            {
                if (_CanSave != value)
                {
                    _CanSave = value;
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        public bool IsNewZone => ZoneId == 0;

        private int _ZoneId;
        public int ZoneId
        {
            get { return _ZoneId; }
            set 
            { 
                if(_ZoneId != value)
                {
                    _ZoneId = value;
                    NotifyOfPropertyChange(nameof(ZoneId));
                } 
            }
        }
        private string? _ZoneName;
        public string ZoneName
        {
            get { return _ZoneName; }
            set
            {
                if (_ZoneName != value)
                {
                    _ZoneName = value;
                    NotifyOfPropertyChange(nameof(ZoneName));
                    CheckSave();
                }
            }
        }
        private bool _ZoneIsActive= true;
        public bool ZoneIsActive
        {
            get { return _ZoneIsActive; }
            set
            {
                if (_ZoneIsActive != value)
                {
                    _ZoneIsActive = value;
                    NotifyOfPropertyChange(nameof(ZoneIsActive));
                }
            }
        }
        private ICommand? _goBackCommand;

        public ICommand? GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                ZoneName = ZoneName;
                return _goBackCommand;
            }
        }

        private ICommand? _saveCommand;

        public ICommand? SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public async Task SaveAsync()
        {
            try
            {
                ZoneGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewZone)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ZoneCreateMessage() { CreateZone = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ZoneUpdateMessage() { UpdateZone = result });
                }
                this.ZoneName = null;
                await Context.ActivateMasterViewAsync();
            }
            catch(Exception ex) 
            {
                throw;
            }
        }

        public void CheckSave()
        {
            CanSave = !string.IsNullOrEmpty(ZoneName);
        }
        public async Task<ZoneGraphQLModel> ExecuteSaveAsync()
        {
            try
            {
                string query = IsNewZone ? @"mutation($data: CreateZoneInput!){
                    CreateResponse: createZone(data:  $data){
                        name
                        isActive
                    }
                }" : @"mutation($id: Int!, $data: UpdateZoneInput!){
                    updateZone(id: $id, data: $data){
                        name
                        isActive
                    }
                }";
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                if (!IsNewZone) variables.id = ZoneId;
                variables.data.name = ZoneName;
                variables.data.isActive = ZoneIsActive;
                var result = IsNewZone ? await ZoneService.Create(query, variables) : await ZoneService.Update(query, variables);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task GoBackAsync()
        {
            this.ZoneName = null;
            await Context.ActivateMasterViewAsync();
        }
    }
}
