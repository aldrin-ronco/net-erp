using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Dynamic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.DianCertificate.ViewModels
{
    public class DianCertificateDetailViewModel : Screen
    {
        #region Dependencies

        private readonly IRepository<DianCertificateGraphQLModel> _dianCertificateService;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Dialog Size

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 600;

        #endregion

        #region State

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public bool IsExtracted
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsExtracted));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region File Selection

        public string FilePath
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilePath));
                    NotifyOfPropertyChange(nameof(CanExtract));
                }
            }
        } = string.Empty;

        public string FilePassword
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilePassword));
                    NotifyOfPropertyChange(nameof(CanExtract));
                }
            }
        } = string.Empty;

        public bool CanExtract => !string.IsNullOrEmpty(FilePath) && !string.IsNullOrEmpty(FilePassword);

        #endregion

        #region Certificate Properties

        public string CertificatePem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CertificatePem));
                }
            }
        } = string.Empty;

        public string PrivateKeyPem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PrivateKeyPem));
                }
            }
        } = string.Empty;

        public string SerialNumber
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SerialNumber));
                }
            }
        } = string.Empty;

        public string Issuer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Issuer));
                }
            }
        } = string.Empty;

        public string Subject
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Subject));
                }
            }
        } = string.Empty;

        public DateTime? ValidFrom
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ValidFrom));
                }
            }
        }

        public DateTime? ValidTo
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ValidTo));
                }
            }
        }

        #endregion

        #region Button States

        public bool CanSave => IsExtracted;

        #endregion

        #region Commands

        private ICommand? _browseCommand;
        public ICommand BrowseCommand
        {
            get
            {
                _browseCommand ??= new DelegateCommand(BrowseFile);
                return _browseCommand;
            }
        }

        private ICommand? _extractCommand;
        public ICommand ExtractCommand
        {
            get
            {
                _extractCommand ??= new AsyncCommand(ExtractCertificateAsync);
                return _extractCommand;
            }
        }

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Constructor

        public DianCertificateDetailViewModel(
            IRepository<DianCertificateGraphQLModel> dianCertificateService,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory)
        {
            _dianCertificateService = dianCertificateService;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;
        }

        #endregion

        #region File Browse & Extract

        private void BrowseFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new()
            {
                Filter = "Certificados PKCS#12 (*.p12;*.pfx)|*.p12;*.pfx",
                Title = "Seleccionar certificado digital"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
            }
        }

        public async Task ExtractCertificateAsync()
        {
            try
            {
                IsBusy = true;

                var (certPem, keyPem, serial, issuer, subject, validFrom, validTo) =
                    ExtractFromP12(FilePath, FilePassword);

                CertificatePem = certPem;
                PrivateKeyPem = keyPem;
                SerialNumber = serial;
                Issuer = issuer;
                Subject = subject;
                ValidFrom = validFrom;
                ValidTo = validTo;
                IsExtracted = true;
            }
            catch (CryptographicException)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    "No se pudo abrir el certificado. Verifique que la contraseña sea correcta y que el archivo sea un certificado PKCS#12 válido.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al extraer el certificado: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static (string certPem, string keyPem, string serial, string issuer, string subject, DateTime validFrom, DateTime validTo)
            ExtractFromP12(string filePath, string password)
        {
            using X509Certificate2 cert = X509CertificateLoader.LoadPkcs12FromFile(filePath, password, X509KeyStorageFlags.Exportable);

            string certPem = cert.ExportCertificatePem();

            string keyPem;
            using (RSA? rsa = cert.GetRSAPrivateKey())
            {
                if (rsa != null)
                {
                    keyPem = new string(PemEncoding.Write("PRIVATE KEY", rsa.ExportPkcs8PrivateKey()));
                }
                else
                {
                    using ECDsa? ecdsa = cert.GetECDsaPrivateKey();
                    keyPem = ecdsa != null
                        ? new string(PemEncoding.Write("PRIVATE KEY", ecdsa.ExportPkcs8PrivateKey()))
                        : throw new CryptographicException("No se encontró una clave privada compatible (RSA o ECDSA) en el certificado.");
                }
            }

            return (certPem, keyPem, cert.SerialNumber, cert.Issuer, cert.Subject, cert.NotBefore, cert.NotAfter);
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                var (fragment, query) = _createQuery.Value;
                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "input", new
                    {
                        certificatePem = CertificatePem,
                        privateKeyPem = PrivateKeyPem,
                        serialNumber = SerialNumber,
                        issuer = Issuer,
                        subject = Subject,
                        validFrom = ValidFrom?.ToString("o"),
                        validTo = ValidTo?.ToString("o")
                    })
                    .Build();

                UpsertResponseType<DianCertificateGraphQLModel> result = await _dianCertificateService.CreateAsync<UpsertResponseType<DianCertificateGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        title: $"{result.Message}!",
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new DianCertificateCreateMessage { CreatedCertificate = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<DianCertificateGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "certificate", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.SerialNumber)
                    .Field(e => e.Issuer)
                    .Field(e => e.Subject)
                    .Field(e => e.ValidFrom)
                    .Field(e => e.ValidTo))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createDianCertificate",
                [new("input", "CreateDianCertificateInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
