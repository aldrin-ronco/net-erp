using Common.Interfaces;
using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Books.DAL.SQLServer
{
    public class AccountingAccountService : IGenericDataAccess<AccountingAccountGraphQLModel>
    {
        public Task<AccountingAccountGraphQLModel> Create(string query, object variables)
        {
            throw new NotImplementedException();
        }

        public Task<AccountingAccountGraphQLModel> Delete(string query, object variables)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<AccountingAccountGraphQLModel>> GetList(string query, object variables, AccountingAccountGraphQLModel placeHolder)
        {
            throw new NotImplementedException();
        }

        public Task<IGenericDataAccess<AccountingAccountGraphQLModel>.PageResponseType> GetPage(string query, object variables)
        {
            throw new NotImplementedException();
        }

        public Task<AccountingAccountGraphQLModel> Update(string query, object variables)
        {
            throw new NotImplementedException();
        }
    }
}
