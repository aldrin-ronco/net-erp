using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dictionaries
{
    public class TreasuryDictionaries
    {
        public static Dictionary<char, string> BankAccountTypeDictionary = new Dictionary<char, string>()
        {
            {'D',"DÉBITO" },
            {'C',"CRÉDITO" }
        };
    }
}
