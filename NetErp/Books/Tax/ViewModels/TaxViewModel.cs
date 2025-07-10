using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Books.Tax.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxViewModel : Conductor<object>.Collection.OneActive
    {
        public IEventAggregator EventAggregator { get; private set; }
        public IMapper AutoMapper { get; private set; }
        private TaxMasterViewModel _TaxMasterViewModel;

        public TaxMasterViewModel TaxMasterViewModel
        {
            get
            {
                if (_TaxMasterViewModel is null) _TaxMasterViewModel = new TaxMasterViewModel(this);
                return _TaxMasterViewModel;
            }
        }
        
        public string listquery = @"
			    query($filter : TaxFilterInput!){
     
                   pageResponse : taxPage(filter : $filter){
        
                       count,
                       rows {
                        id
                        name
                        margin
                        taxType {
                          id
                          name
                        }
                        generatedTaxAccount {id name},
                        generatedTaxRefundAccount {id name},
                        deductibleTaxAccount {id name},
                        deductibleTaxRefundAccount { id name}
                        isActive
                        formula
                        alternativeFormula
                       }
      
      
                    }
       
                }
                ";
        public TaxViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterViewModelAsync();
                }
                catch (AsyncException ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message ?? ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }
        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(TaxMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEdit(TaxGraphQLModel? entity)
        {
            TaxDetailViewModel instance = new(this, entity);


            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
