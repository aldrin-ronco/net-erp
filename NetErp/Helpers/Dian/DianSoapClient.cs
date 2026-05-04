using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.Dian.Internal;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Dian
{
    /// <summary>
    /// Implementación de <see cref="IDianSoapClient"/>. Carga la configuración DIAN activa
    /// (scope INVOICE / PRODUCTION) y el certificado vigente vía GraphQL — los cachea por
    /// proceso con doble check + lock — y orquesta firma, transporte y parseo para cada
    /// operación.
    /// </summary>
    public class DianSoapClient(
        IRepository<DianSoftwareConfigGraphQLModel> configRepo,
        IRepository<DianCertificateGraphQLModel> certificateRepo) : IDianSoapClient
    {
        private const string SoapActionPrefix = "http://wcf.dian.colombia/IWcfDianCustomerServices/";

        private readonly IRepository<DianSoftwareConfigGraphQLModel> _configRepo = configRepo;
        private readonly IRepository<DianCertificateGraphQLModel> _certificateRepo = certificateRepo;

        private readonly Lock _prerequisitesLock = new();
        private DianSoftwareConfigGraphQLModel? _cachedConfig;
        private DianCertificateGraphQLModel? _cachedCertificate;

        public async Task<TResponse> ExecuteAsync<TResponse>(
            IDianOperation<TResponse> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            (DianSoftwareConfigGraphQLModel config, DianCertificateGraphQLModel certificate) =
                await EnsurePrerequisitesAsync().ConfigureAwait(false);

            string body = operation.BuildRequestBody(config);
            string action = SoapActionPrefix + operation.OperationName;

            return await Task.Run(() =>
            {
                string signed = DianMessageSigner.Sign(action, config.ServiceUrl, body, certificate.CertificatePem, certificate.PrivateKeyPem);
                string responseXml = DianHttpTransport.Post(signed, config.WsdlUrl);
                return operation.ParseResponse(responseXml);
            }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(DianSoftwareConfigGraphQLModel Config, DianCertificateGraphQLModel Certificate)> EnsurePrerequisitesAsync()
        {
            DianSoftwareConfigGraphQLModel? config;
            DianCertificateGraphQLModel? cert;

            lock (_prerequisitesLock)
            {
                config = _cachedConfig;
                cert = _cachedCertificate;
            }

            if (config != null && cert != null) return (config, cert);

            DianSoftwareConfigGraphQLModel? loadedConfig;
            DianCertificateGraphQLModel? loadedCert;
            try
            {
                loadedConfig = await LoadActiveConfigAsync().ConfigureAwait(false);
                loadedCert = await LoadActiveCertificateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new DianConfigurationException($"No fue posible cargar los prerrequisitos DIAN: {ex.Message}");
            }

            if (loadedConfig == null)
                throw new DianConfigurationException("No hay configuración DIAN activa para facturación electrónica. Configure el software DIAN antes de consultar.");
            if (loadedCert == null)
                throw new DianConfigurationException("No hay certificado DIAN configurado. Cargue el certificado vigente antes de consultar.");

            lock (_prerequisitesLock)
            {
                _cachedConfig = loadedConfig;
                _cachedCertificate = loadedCert;
            }

            return (loadedConfig, loadedCert);
        }

        private async Task<DianSoftwareConfigGraphQLModel?> LoadActiveConfigAsync()
        {
            (GraphQLQueryFragment fragment, string query) = _dianConfigQuery.Value;

            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "environment", "PRODUCTION")
                .For(fragment, "documentCategory", "INVOICE")
                .Build();

            DianSoftwareConfigByScopeDataContext context = await _configRepo.GetDataContextAsync<DianSoftwareConfigByScopeDataContext>(query, variables);
            DianSoftwareConfigGraphQLModel? config = context?.DianSoftwareConfigByScope;
            return config == null || config.Id < 1 ? null : config;
        }

        private async Task<DianCertificateGraphQLModel?> LoadActiveCertificateAsync()
        {
            (_, string query) = _dianCertQuery.Value;
            ActiveDianCertificateDataContext context = await _certificateRepo.GetDataContextAsync<ActiveDianCertificateDataContext>(query, new { });
            DianCertificateGraphQLModel? cert = context?.ActiveDianCertificate;
            return cert == null || cert.Id < 1 ? null : cert;
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _dianConfigQuery = new(() =>
        {
            var fields = FieldSpec<DianSoftwareConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Environment)
                .Field(f => f.DocumentCategory)
                .Field(f => f.ProviderNit)
                .Field(f => f.SoftwareId)
                .Field(f => f.ServiceUrl)
                .Field(f => f.WsdlUrl)
                .Build();

            var fragment = new GraphQLQueryFragment(
                "dianSoftwareConfigByScope",
                [new("environment", "DianEnvironment!"), new("documentCategory", "DianDocumentCategory!")],
                fields);
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _dianCertQuery = new(() =>
        {
            var fields = FieldSpec<DianCertificateGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.CertificatePem)
                .Field(f => f.PrivateKeyPem)
                .Build();

            var fragment = new GraphQLQueryFragment("activeDianCertificate", [], fields);
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });
    }
}
