using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Dynamic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.DianCertificate.ViewModels
{
    public class DianCertificateDetailViewModel : Screen
    {
        #region Dependencies

        private readonly IRepository<DianCertificateGraphQLModel> _dianCertificateService;
        private readonly IEventAggregator _eventAggregator;

        #endregion

        #region State

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private bool _isExtracted;
        public bool IsExtracted
        {
            get => _isExtracted;
            set
            {
                if (_isExtracted != value)
                {
                    _isExtracted = value;
                    NotifyOfPropertyChange(nameof(IsExtracted));
                    NotifyOfPropertyChange(nameof(CertificateInfoVisibility));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Visibility

        public Visibility CertificateInfoVisibility => IsExtracted ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        #region File Selection

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    NotifyOfPropertyChange(nameof(FilePath));
                    NotifyOfPropertyChange(nameof(CanExtract));
                }
            }
        }

        private string _filePassword = string.Empty;
        public string FilePassword
        {
            get => _filePassword;
            set
            {
                if (_filePassword != value)
                {
                    _filePassword = value;
                    NotifyOfPropertyChange(nameof(FilePassword));
                    NotifyOfPropertyChange(nameof(CanExtract));
                }
            }
        }

        public bool CanExtract => !string.IsNullOrEmpty(FilePath) && !string.IsNullOrEmpty(FilePassword);

        #endregion

        #region Certificate Properties

        private string _certificatePem = string.Empty;
        public string CertificatePem
        {
            get => _certificatePem;
            set
            {
                if (_certificatePem != value)
                {
                    _certificatePem = value;
                    NotifyOfPropertyChange(nameof(CertificatePem));
                }
            }
        }

        private string _privateKeyPem = string.Empty;
        public string PrivateKeyPem
        {
            get => _privateKeyPem;
            set
            {
                if (_privateKeyPem != value)
                {
                    _privateKeyPem = value;
                    NotifyOfPropertyChange(nameof(PrivateKeyPem));
                }
            }
        }

        private string _serialNumber = string.Empty;
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (_serialNumber != value)
                {
                    _serialNumber = value;
                    NotifyOfPropertyChange(nameof(SerialNumber));
                }
            }
        }

        private string _issuer = string.Empty;
        public string Issuer
        {
            get => _issuer;
            set
            {
                if (_issuer != value)
                {
                    _issuer = value;
                    NotifyOfPropertyChange(nameof(Issuer));
                }
            }
        }

        private string _subject = string.Empty;
        public string Subject
        {
            get => _subject;
            set
            {
                if (_subject != value)
                {
                    _subject = value;
                    NotifyOfPropertyChange(nameof(Subject));
                }
            }
        }

        private DateTime? _validFrom;
        public DateTime? ValidFrom
        {
            get => _validFrom;
            set
            {
                if (_validFrom != value)
                {
                    _validFrom = value;
                    NotifyOfPropertyChange(nameof(ValidFrom));
                }
            }
        }

        private DateTime? _validTo;
        public DateTime? ValidTo
        {
            get => _validTo;
            set
            {
                if (_validTo != value)
                {
                    _validTo = value;
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
            IEventAggregator eventAggregator)
        {
            _dianCertificateService = dianCertificateService;
            _eventAggregator = eventAggregator;
        }

        #endregion

        #region File Browse & Extract

        private void BrowseFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Certificados PKCS#12 (*.p12;*.pfx)|*.p12;*.pfx",
                Title = "Seleccionar certificado digital"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
            }
        }

        public Task ExtractCertificateAsync()
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
                ThemedMessageBox.Show("Atención !",
                    "No se pudo abrir el certificado. Verifique que la contraseña sea correcta y que el archivo sea un certificado PKCS#12 válido.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención !",
                    $"Error al extraer el certificado: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
            return Task.CompletedTask;
        }

        private static (string certPem, string keyPem, string serial, string issuer, string subject, DateTime validFrom, DateTime validTo)
            ExtractFromP12(string filePath, string password)
        {
            using var cert = new X509Certificate2(filePath, password, X509KeyStorageFlags.Exportable);

            string certPem = cert.ExportCertificatePem();

            string keyPem;
            using (var rsa = cert.GetRSAPrivateKey())
            {
                if (rsa != null)
                {
                    keyPem = new string(PemEncoding.Write("PRIVATE KEY", rsa.ExportPkcs8PrivateKey()));
                }
                else
                {
                    using var ecdsa = cert.GetECDsaPrivateKey();
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

                string query = _createQuery.Value;
                dynamic variables = new ExpandoObject();
                variables.createResponseInput = new ExpandoObject();
                variables.createResponseInput.certificatePem = CertificatePem;
                variables.createResponseInput.privateKeyPem = PrivateKeyPem;
                variables.createResponseInput.serialNumber = SerialNumber;
                variables.createResponseInput.issuer = Issuer;
                variables.createResponseInput.subject = Subject;
                variables.createResponseInput.validFrom = ValidFrom?.ToString("o");
                variables.createResponseInput.validTo = ValidTo?.ToString("o");

                UpsertResponseType<DianCertificateGraphQLModel> result = await _dianCertificateService.CreateAsync<UpsertResponseType<DianCertificateGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        title: $"{result.Message}!",
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new DianCertificateCreateMessage { CreatedCertificate = result });

                await TryCloseAsync(true);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
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

        private static readonly Lazy<string> _createQuery = new(() =>
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

            var parameter = new GraphQLQueryParameter("input", "CreateDianCertificateInput!");
            var fragment = new GraphQLQueryFragment("createDianCertificate", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion
    }
}
