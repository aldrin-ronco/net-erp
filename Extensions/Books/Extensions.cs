
using DTOLibrary.Books;
using Models.Books;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Extensions.Books
{
    public static class Extensions
    {
        public static void Replace(this List<AccountingAccountGraphQLModel> accounts, AccountingAccountGraphQLModel updatedAccount)
        {
            AccountingAccountGraphQLModel? accountToReplace = accounts.FirstOrDefault(accountingAccount => accountingAccount.Id == updatedAccount.Id);
            if (accountToReplace != null)
            {
                int index = accounts.IndexOf(accountToReplace);
                accounts.Remove(accountToReplace);
                accounts.Insert(index, updatedAccount);
            }
        }
        public static void Replace(this ObservableCollection<AccountingEntityDTO> entities, AccountingEntityDTO updatedEntity)
        {
            AccountingEntityDTO entityToReplace = entities.FirstOrDefault(x => x.Id == updatedEntity.Id);
            if (entityToReplace != null)
            {
                int index = entities.IndexOf(entityToReplace);
                entities.Remove(entityToReplace);
                entities.Insert(index, updatedEntity);
            }
        }

        public static void RemoveById(this ObservableCollection<AccountingEntityDTO> accountingEntities, int id)
        {
            AccountingEntityDTO accountingEntityToDelete = accountingEntities.Where(x => x.Id == id).FirstOrDefault();
            if(accountingEntityToDelete != null)
            {
                accountingEntities.Remove(accountingEntityToDelete);
            };
        }
    }
}
