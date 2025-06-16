using Caliburn.Micro;
using Common.Interfaces;
using Models.Global;
using NetErp.Global.AuthorizationSequence.ViewModels;
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
    public class ParameterMasterViewModel : Screen
    {
        public ParameterViewModel Context { get; set; }
        public readonly IGenericDataAccess<ParameterGraphQLModel> ParameterSequenceService = IoC.Get<IGenericDataAccess<ParameterGraphQLModel>>();

        public ParameterMasterViewModel(ParameterViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            
            _ = Task.Run(() => InitializeAsync());
        }
        private BindableCollection<ItemViewModel> _actions { get; set; }
        public BindableCollection<ItemViewModel> Actions
        {
            get { return _actions; }
            set
            {
                if (_actions != value)
                {
                    _actions = value;
                    NotifyOfPropertyChange(nameof(Actions));
                }
            }
        }
        public async Task InitializeAsync()
        {
            try
            {
                string query = @"
                            query( ){
                            ListResponse : parameters(){
        
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
                Actions = Context.AutoMapper.Map<BindableCollection<ItemViewModel>>(parameters);

                var a = 1;
            } catch (Exception ex)
            {
                throw;
            }
           
        }

        public async Task save()
        {
            var a = Actions;

        }
     }
}
