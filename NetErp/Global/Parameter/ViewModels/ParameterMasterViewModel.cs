using Caliburn.Micro;
using Common.Interfaces;
using Models.Global;
using NetErp.Global.AuthorizationSequence.ViewModels;
using NetErp.Global.DynamicControl;
using NetErp.Global.DynamicControl.ViewModels;
using Ninject.Activation;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.Parameter.ViewModels
{
    public class ParameterMasterViewModel : Conductor<Screen>
    {
        public ParameterViewModel Context { get; set; }
        public readonly IGenericDataAccess<ParameterGraphQLModel> ParameterSequenceService = IoC.Get<IGenericDataAccess<ParameterGraphQLModel>>();
        public DynamicControlViewModel DynamicControlBook { get; set; }
        public DynamicControlViewModel DynamicControlInventory { get; set; }
        public DynamicControlViewModel DynamicControTreasury { get; set; }
        public DynamicControlViewModel DynamicControlBilling { get; set; }
        public DynamicControlViewModel DynamicControlGlobal { get; set; }
        public ParameterMasterViewModel(ParameterViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            DynamicControlBook = new DynamicControlViewModel();
            DynamicControlInventory = new DynamicControlViewModel();
            DynamicControTreasury = new DynamicControlViewModel();
            DynamicControlBilling = new DynamicControlViewModel();
            DynamicControlGlobal = new DynamicControlViewModel();

            _ = Task.Run(() => InitializeAsync());
        }
        private int _selectedIndex = 0;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }
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
        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
                            query( ){
                            ListResponse : configurationParameters(){
        
                                id
                                name
                                code
                                value
                                moduleId
                                datatype {
                                  id
                                  name
                                }
                                qualifiers {
                                  id
                                  name
                                  qualifierTypeId
                                }
                                qualifierScreens : qualifiers { 
                                id
                                  name
                                  qualifierTypeId
                               
                                }
                               
                             }
       
                         }";
                dynamic variables = new ExpandoObject();
                var parameters = await ParameterSequenceService.GetList(query, variables);
                //Actions = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(parameters);
                BindableCollection<DynamicControlModel> controls = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(parameters);
                DynamicControlBilling.Controls = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(controls.Where(f => f.ModuleId == 2));
                DynamicControlGlobal.Controls = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(controls.Where(f => f.ModuleId == 7));
                DynamicControTreasury.Controls = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(controls.Where(f => f.ModuleId == 3));
                DynamicControlBook.Controls = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(controls.Where(f => f.ModuleId == 5));
                DynamicControlInventory.Controls = Context.AutoMapper.Map<BindableCollection<DynamicControlModel>>(controls.Where(f => f.ModuleId == 1));


            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task save()
        {
           

        }
     }
}
