using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Resolver de permisos del usuario actual. NO hace HTTP por sí mismo — depende de
    /// <see cref="PermissionDefinitionCache"/>, <see cref="CompanyPermissionDefaultCache"/>
    /// y <see cref="UserPermissionCache"/> para obtener los datos.
    /// Aplica la cascada: <c>UserPermission &gt; CompanyPermissionDefault &gt; SystemDefault</c>
    /// y expone lookups O(1) vía <see cref="IsAllowed"/>, <see cref="IsRequired"/>, etc.
    ///
    /// Diseño: al separar el fetching de la lógica de cascada, las 3 sub-caches pueden
    /// participar en batches combinados con otros caches (via <see cref="CacheBatchLoader"/>),
    /// reduciendo round-trips HTTP en módulos que cargan múltiples caches a la vez.
    /// Cleared on logout/company switch via <see cref="IEntityCache"/>.
    /// </summary>
    public class PermissionCache : IEntityCache,
        IHandle<CompanyPermissionDefaultChangedMessage>,
        IHandle<UserPermissionChangedMessage>
    {
        private readonly PermissionDefinitionCache _permissionDefinitionCache;
        private readonly CompanyPermissionDefaultCache _companyPermissionDefaultCache;
        private readonly UserPermissionCache _userPermissionCache;
        private readonly IGraphQLClient _graphQLClient;
        private readonly IEventAggregator _eventAggregator;

        private readonly Lock _lock = new();
        private readonly Dictionary<string, ResolvedPermission> _permissions = [];
        private bool _isLoaded;

        public bool IsInitialized => _isLoaded;

        public PermissionCache(
            PermissionDefinitionCache permissionDefinitionCache,
            CompanyPermissionDefaultCache companyPermissionDefaultCache,
            UserPermissionCache userPermissionCache,
            IGraphQLClient graphQLClient,
            IEventAggregator eventAggregator)
        {
            _permissionDefinitionCache = permissionDefinitionCache;
            _companyPermissionDefaultCache = companyPermissionDefaultCache;
            _userPermissionCache = userPermissionCache;
            _graphQLClient = graphQLClient;
            _eventAggregator = eventAggregator;
            _eventAggregator.SubscribeOnUIThread(this);
        }

        /// <summary>
        /// Carga los datos necesarios (si no están ya cargados) y ejecuta la cascada de resolución.
        /// Si las 3 sub-caches ya fueron cargadas por un batch externo, esto solo ejecuta la cascada
        /// (sin HTTP). Si no, dispara 1 HTTP combinado con los 3 fragments via <see cref="CacheBatchLoader"/>.
        /// </summary>
        public async Task EnsureLoadedAsync()
        {
            lock (_lock) { if (_isLoaded) return; }

            // Carga batch de las 3 sub-caches (no-op si ya están inicializadas por un batch externo)
            await CacheBatchLoader.LoadAsync(
                _graphQLClient,
                default,
                _permissionDefinitionCache,
                _companyPermissionDefaultCache,
                _userPermissionCache);

            ResolveCascade();
        }

        /// <summary>
        /// Ejecuta la cascada de resolución a partir de los datos en las 3 sub-caches.
        /// No hace HTTP. Pre-requisito: las 3 sub-caches deben estar cargadas.
        /// </summary>
        private void ResolveCascade()
        {
            // Index company defaults and user permissions by permissionDefinition.Id
            Dictionary<int, string> companyDefaultsByPermId = _companyPermissionDefaultCache.Items
                .Where(cd => cd.PermissionDefinition != null)
                .GroupBy(cd => cd.PermissionDefinition!.Id)
                .ToDictionary(g => g.Key, g => g.First().DefaultValue);

            Dictionary<int, UserPermissionGraphQLModel> userPermsByPermId = _userPermissionCache.Items
                .Where(up => up.PermissionDefinition != null)
                .GroupBy(up => up.PermissionDefinition!.Id)
                .ToDictionary(g => g.Key, g => g.First());

            // Resolve cascade for each permission definition
            lock (_lock)
            {
                _permissions.Clear();

                foreach (PermissionDefinitionGraphQLModel permDef in _permissionDefinitionCache.Items)
                {
                    string effectiveValue = permDef.SystemDefault;

                    // Level 2: Company default overrides system default
                    if (companyDefaultsByPermId.TryGetValue(permDef.Id, out string? companyValue))
                        effectiveValue = companyValue;

                    // Level 1: User permission overrides everything (if not expired)
                    if (userPermsByPermId.TryGetValue(permDef.Id, out UserPermissionGraphQLModel? userPerm))
                    {
                        bool isExpired = false;

                        if (!string.IsNullOrEmpty(userPerm.ExpiresAt) &&
                            DateTime.TryParse(userPerm.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                        {
                            isExpired = parsed < DateTime.UtcNow;
                        }

                        if (!isExpired)
                            effectiveValue = userPerm.Value;
                    }

                    _permissions[permDef.Code] = new ResolvedPermission(
                        permDef.Code,
                        permDef.PermissionType,
                        effectiveValue);
                }

                _isLoaded = true;
            }
        }

        /// <summary>
        /// Returns true if the ACTION permission is allowed.
        /// SystemAdmin always returns true. Unknown codes return true (permissive by default).
        /// </summary>
        public bool IsAllowed(string code)
        {
            if (SessionInfo.IsSystemAdmin) return true;

            lock (_lock)
            {
                if (_permissions.TryGetValue(code, out ResolvedPermission? perm))
                    return perm.EffectiveValue == "ALLOWED";
            }

            return true;
        }

        /// <summary>
        /// Returns true if the ACTION permission is denied.
        /// </summary>
        public bool IsDenied(string code) => !IsAllowed(code);

        /// <summary>
        /// Returns true if the FIELD permission is required.
        /// SystemAdmin always returns false. Unknown codes return false (optional by default).
        /// </summary>
        public bool IsRequired(string code)
        {
            if (SessionInfo.IsSystemAdmin) return false;

            lock (_lock)
            {
                if (_permissions.TryGetValue(code, out ResolvedPermission? perm))
                    return perm.EffectiveValue == "REQUIRED";
            }

            return false;
        }

        /// <summary>
        /// Returns true if the FIELD permission is optional.
        /// </summary>
        public bool IsOptional(string code) => !IsRequired(code);

        /// <summary>
        /// Returns true if a permission with the given code exists in the cache.
        /// </summary>
        public bool HasPermission(string code)
        {
            lock (_lock)
            {
                return _permissions.ContainsKey(code);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _permissions.Clear();
                _isLoaded = false;
            }
        }

        /// <summary>
        /// Forces a full reload of permissions (clears sub-caches + re-batches + re-resolves)
        /// and notifies all listeners via <see cref="PermissionsCacheRefreshedMessage"/>.
        /// </summary>
        public async Task ReloadAsync()
        {
            // Clear local state + sub-caches
            _permissionDefinitionCache.Clear();
            _companyPermissionDefaultCache.Clear();
            _userPermissionCache.Clear();
            Clear();

            await EnsureLoadedAsync();
            await _eventAggregator.PublishOnCurrentThreadAsync(new PermissionsCacheRefreshedMessage());
        }

        public async Task HandleAsync(CompanyPermissionDefaultChangedMessage message, CancellationToken cancellationToken)
        {
            await ReloadAsync();
        }

        public async Task HandleAsync(UserPermissionChangedMessage message, CancellationToken cancellationToken)
        {
            await ReloadAsync();
        }

        #region Internal Types

        private record ResolvedPermission(string Code, string PermissionType, string EffectiveValue);

        #endregion
    }
}
