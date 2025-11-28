using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingPresentationGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public bool AllowsClosure { get; set; } = false;
        public AccountingBookGraphQLModel? ClosureAccountingBook { get; set; }
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks { get; set; } = [];

        public override string ToString()
        {
            return Name;
        }
    }
    public class AccountingPresentationDataContext
    {
        public PageType<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; } 
        public PageType<AccountingBookGraphQLModel> AccountingBooks { get; set; } 
    }
    public class AccountingPresentationCreateMessage
    {
        public UpsertResponseType<AccountingPresentationGraphQLModel> CreatedAccountingPresentation { get; set; } = new();
    }
    public class AccountingPresentationUpdateMessage
    {
        public UpsertResponseType<AccountingPresentationGraphQLModel> UpdatedAccountingPresentation { get; set; } = new();
    }

    public class AccountingPresentationDeleteMessage
    {
        public DeleteResponseType DeletedAccountingPresentation { get; set; } = new();
    }
}
