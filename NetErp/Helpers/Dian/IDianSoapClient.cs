using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Dian
{
    /// <summary>
    /// Cliente único para invocar cualquier operación SOAP del WS DIAN
    /// (https://vpfe.dian.gov.co/WcfDianCustomerServices.svc).
    ///
    /// Análogo a <c>IGraphQLClient</c>: el cliente es agnóstico a la operación,
    /// recibe un <see cref="IDianOperation{TResponse}"/> que sabe armar su body
    /// y parsear su response, y se encarga de:
    /// 1) cargar configuración + certificado activos (cacheados por proceso),
    /// 2) firmar el envelope con WS-Security X.509 (RSA-SHA256, c14n exclusiva),
    /// 3) hacer el POST SOAP 1.2 al endpoint DIAN,
    /// 4) entregar el XML al operation para deserializar.
    ///
    /// Si no hay configuración o certificado activo, lanza <see cref="DianConfigurationException"/>.
    /// </summary>
    public interface IDianSoapClient
    {
        Task<TResponse> ExecuteAsync<TResponse>(
            IDianOperation<TResponse> operation,
            CancellationToken cancellationToken = default);
    }
}
