using Common.Helpers;
using Common.Interfaces;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Implementación singleton que cachea imágenes por <c>itemId</c> para toda
    /// la sesión. Inicializa S3 lazy en la primera invocación. Implementa
    /// <see cref="IEntityCache"/> para que ShellViewModel limpie en
    /// logout/cambio de empresa.
    /// </summary>
    public class ItemImageProvider : IItemImageProvider, IEntityCache
    {
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3LocationService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly Dictionary<int, ObservableCollection<ImageByItemDTO>> _cache = [];
        private readonly object _cacheGate = new();
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private S3Helper? _s3Helper;
        private string _localCachePath = string.Empty;
        private bool _initialized;

        public ItemImageProvider(
            IRepository<S3StorageLocationGraphQLModel> s3LocationService,
            JoinableTaskFactory joinableTaskFactory)
        {
            _s3LocationService = s3LocationService ?? throw new ArgumentNullException(nameof(s3LocationService));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
        }

        public bool IsInitialized => _initialized;

        public bool IsAvailable => _initialized && _s3Helper != null && !string.IsNullOrEmpty(_localCachePath);

        public async Task<IReadOnlyList<ImageByItemDTO>> GetImagesAsync(ItemGraphQLModel item, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Id <= 0) return [];

            await EnsureInitializedAsync().ConfigureAwait(false);

            // Cache hit
            ObservableCollection<ImageByItemDTO>? cached;
            lock (_cacheGate) _cache.TryGetValue(item.Id, out cached);
            if (cached != null) return cached;

            // Build DTOs desde sources del item
            ImageByItemGraphQLModel[] sources = item.Images?.OrderBy(i => i.DisplayOrder).ToArray() ?? [];
            ObservableCollection<ImageByItemDTO> images;
            if (sources.Length == 0 || !IsAvailable)
            {
                images = [];
            }
            else
            {
                images =
                [
                    .. sources.Select(s => new ImageByItemDTO
                    {
                        DisplayOrder = s.DisplayOrder,
                        S3Bucket = s.S3Bucket,
                        S3BucketDirectory = s.S3BucketDirectory,
                        S3FileName = s.S3FileName
                    })
                ];
            }
            lock (_cacheGate) _cache[item.Id] = images;

            // Fire-and-forget download — DTOs notifican SourceImage cuando lleguen.
            // NO usar el token del caller: si el caller cancela (ej. cambio de selección),
            // la descarga debe completar igual para poblar el cache. Próxima vez que
            // se pida el mismo item, retorna instantáneo con bitmaps ya cargados.
            if (images.Count > 0 && IsAvailable)
                _ = Task.Run(() => DownloadAndLoadAsync(images, CancellationToken.None));

            return images;
        }

        public void Clear()
        {
            lock (_cacheGate) _cache.Clear();
            _s3Helper?.Dispose();
            _s3Helper = null;
            _localCachePath = string.Empty;
            _initialized = false;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;
            await _initGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized) return;
                (_s3Helper, _localCachePath) = await S3ConfigLoader.LoadProductImagesAsync(_s3LocationService).ConfigureAwait(false);
                _initialized = true;
            }
            finally
            {
                _initGate.Release();
            }
        }

        private async Task DownloadAndLoadAsync(ObservableCollection<ImageByItemDTO> images, CancellationToken token)
        {
            if (_s3Helper == null || string.IsNullOrEmpty(_localCachePath)) return;
            try
            {
                List<Task> tasks = [];
                List<string> failed = [];
                foreach (ImageByItemDTO img in images)
                {
                    string localPath = Path.Combine(_localCachePath, img.S3FileName);
                    if (!Path.Exists(localPath))
                        tasks.Add(DownloadOneAsync(img, localPath, failed));
                    else
                        img.ImagePath = localPath;
                }
                if (tasks.Count > 0) await Task.WhenAll(tasks).ConfigureAwait(false);

                // Construir bitmaps en background con Freeze() — cross-thread safe.
                Dictionary<string, BitmapImage> bitmaps = [];
                foreach (ImageByItemDTO img in images)
                {
                    if (failed.Contains(img.S3FileName)) continue;
                    string localPath = Path.Combine(_localCachePath, img.S3FileName);
                    if (Path.Exists(localPath))
                    {
                        bitmaps[img.S3FileName] = LoadBitmap(localPath);
                        img.ImagePath = localPath;
                    }
                }

                // Asignar SourceImage en UI thread vía Dispatcher (Caliburn NotifyOfPropertyChange requiere UI).
                System.Windows.Application? app = System.Windows.Application.Current;
                if (app?.Dispatcher == null) return;
                await app.Dispatcher.InvokeAsync(() =>
                {
                    foreach (ImageByItemDTO img in images)
                    {
                        if (bitmaps.TryGetValue(img.S3FileName, out BitmapImage? bmp)) img.SourceImage = bmp;
                    }
                });
            }
            catch
            {
                // Silenciar — thumbnails fallidos quedan vacíos.
            }
        }

        private async Task DownloadOneAsync(ImageByItemDTO img, string localPath, List<string> failed)
        {
            try { await _s3Helper!.DownloadFileAsync(localPath, img.S3FileName).ConfigureAwait(false); }
            catch { lock (failed) failed.Add(img.S3FileName); }
        }

        private static BitmapImage LoadBitmap(string path)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            if (bitmap.CanFreeze) bitmap.Freeze();
            return bitmap;
        }
    }
}
