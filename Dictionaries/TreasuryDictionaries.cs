using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dictionaries
{
    public class TreasuryDictionaries
    {
        public readonly static Dictionary<char, string> BankAccountTypeDictionary = new()
        {
            {'D',"DÉBITO" },
            {'C',"CRÉDITO" }
        };
    }
}
