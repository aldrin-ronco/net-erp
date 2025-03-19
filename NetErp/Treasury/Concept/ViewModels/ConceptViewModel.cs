﻿using AutoMapper;
using Caliburn.Micro;
using Models.Treasury;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private ConceptMasterViewModel _conceptMasterViewModel;
        public ConceptMasterViewModel ConceptMasterViewModel
        {
            get
            {
                if (_conceptMasterViewModel is null) _conceptMasterViewModel = new ConceptMasterViewModel(this);
                return _conceptMasterViewModel;
            }
        }

        public ConceptViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _ = Task.Run(ActivateMasterView);
        }

        private async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(ConceptMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
