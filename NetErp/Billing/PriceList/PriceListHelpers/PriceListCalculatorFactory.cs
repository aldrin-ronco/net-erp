using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public interface IPriceListCalculatorFactory
    {
        IPriceListCalculator GetCalculator(bool useAlternativeFormula);
    }

    // 3. Implementar el factory con inyección de dependencias
    public class PriceListCalculatorFactory : IPriceListCalculatorFactory
    {
        private readonly IKernel _kernel;

        public PriceListCalculatorFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        public IPriceListCalculator GetCalculator(bool useAlternativeFormula)
        {
            string calculatorName = useAlternativeFormula ? "Alternative" : "Standard";
            return _kernel.Get<IPriceListCalculator>(calculatorName);
        }
    }
}
