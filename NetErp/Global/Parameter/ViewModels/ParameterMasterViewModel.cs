using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.XtraEditors.Filtering;
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
            dynamic variables = new ExpandoObject();
            variables.data = new ExpandoObject();
            switch (SelectedIndex)
            {
               

                case 0: //Contabilidad
                    variables.data.configurationParameters = DynamicControlBook.GetDataControls();
                break;

                case 1: //Inventarios
                    variables.data.configurationParameters = DynamicControlInventory.GetDataControls();
                    break;
                case 2: //Tesoreria
                    variables.data.configurationParameters = DynamicControTreasury.GetDataControls();
                    break;
                case 3: //Ventas
                    variables.data.configurationParameters = DynamicControlBilling.GetDataControls();
                    break;
                case 4:  //Generales
                    variables.data.configurationParameters = DynamicControlGlobal.GetDataControls();
                    break;
            }

            string query = @"mutation($data : UpdateConfigurationParameterInput!) {
                          updateConfigurationParameter(data: $data) {
                            id
                            name
                            code
                            value 
                          }
                        }";
            try
            {
                IsBusy = true;
             var result =  await ParameterSequenceService.Update(query, variables);
                var a = 1;

            }
            catch(Exception ex)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
             
        }
     }
}
