
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
        public static void Replace(this ObservableCollection<AccountingAccountGraphQLModel> accounts, AccountingAccountGraphQLModel UpdatedAccount)
        {
            AccountingAccountGraphQLModel accountToReplace = accounts.FirstOrDefault(x => x.Id == UpdatedAccount.Id);
            if (accountToReplace != null)
            {
                int index = accounts.IndexOf(accountToReplace);
                accounts.Remove(accountToReplace);
                accounts.Insert(index, UpdatedAccount);
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

        public static void Replace(this ObservableCollection<AccountingSourceGraphQLModel> accountingSources, AccountingSourceGraphQLModel updatedAccountingSource)
        {
            AccountingSourceGraphQLModel accountingSoureToReplace = accountingSources.Where(x => x.Id == updatedAccountingSource.Id).FirstOrDefault();
            int index = accountingSources.IndexOf(accountingSoureToReplace);
            Application.Current.Dispatcher.Invoke(() =>
            {
                accountingSources.Remove(accountingSoureToReplace);
                accountingSources.Insert(index, updatedAccountingSource);
            });
        }
        public static void Replace(this ObservableCollection<AccountingEntryDraftMasterDTO> accountingEntriesDraft, AccountingEntryDraftMasterDTO updatedAccountingEntryDraft)
        {
            AccountingEntryDraftMasterDTO accountingEntryDraftToReplace = accountingEntriesDraft.Where(x => x.Id == updatedAccountingEntryDraft.Id).FirstOrDefault();
            int index = accountingEntriesDraft.IndexOf(accountingEntryDraftToReplace);
            Application.Current.Dispatcher.Invoke(() =>
            {
                accountingEntriesDraft.Remove(accountingEntryDraftToReplace);
                accountingEntriesDraft.Insert(index, updatedAccountingEntryDraft);
            });
        }

        public static void Replace(this ObservableCollection<AccountingEntryMasterDTO> accountingEntries, AccountingEntryMasterDTO updatedAccountingEntry)
        {
            AccountingEntryMasterDTO accountingEntryToReplace = accountingEntries.Where(x => x.Id == updatedAccountingEntry.Id).FirstOrDefault();
            int index = accountingEntries.IndexOf(accountingEntryToReplace);
            Application.Current.Dispatcher.Invoke(() =>
            {
                accountingEntries.Remove(accountingEntryToReplace);
                accountingEntries.Insert(index, updatedAccountingEntry);
            });
        }

        public static void Replace(this ObservableCollection<IdentificationTypeDTO> identificationTypes, IdentificationTypeDTO updatedIdentificationType)
        {
            IdentificationTypeDTO identificationTypeToReplace = identificationTypes.Where(x => x.Id == updatedIdentificationType.Id).FirstOrDefault();
            int index = identificationTypes.IndexOf(identificationTypeToReplace);
            Application.Current.Dispatcher.Invoke(() =>
            {
                identificationTypes.Remove(identificationTypeToReplace);
                identificationTypes.Insert(index, updatedIdentificationType);
            });
        }

        public static void RemoveById(this ObservableCollection<AccountingEntityDTO> accountingEntities, int id)
        {
            AccountingEntityDTO accountingEntityToDelete = accountingEntities.Where(x => x.Id == id).FirstOrDefault();
            if(accountingEntityToDelete != null)
            {
                accountingEntities.Remove(accountingEntityToDelete);
            };
        }

        public static void RemoveById(this ObservableCollection<IdentificationTypeDTO> identificationTypes, int id)
        {
            IdentificationTypeDTO identificationTypeToDelete = identificationTypes.Where(x => x.Id == id).FirstOrDefault();
            Application.Current.Dispatcher.Invoke(() =>
            {
                identificationTypes.Remove(identificationTypeToDelete);
            });
        }
    }
}
