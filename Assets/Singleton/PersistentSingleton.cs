using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Core.Singleton
{
    /// <summary>
    /// Base class for persistent singletons that survive scene transitions.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class</typeparam>
    /// <remarks>
    /// Usage patterns:
    /// 1. Synchronous access (when no async initialization is needed):
    ///    - Access via Instance property
    ///    - Set InitializationTiming.Immediate if setup is required
    /// 
    /// 2. Asynchronous access (when async initialization is needed):
    ///    - Use WaitForReadyAsync() to ensure full initialization
    ///    - Override OnInitializeAsync() for setup logic
    ///    
    /// Thread safety:
    /// - Enable ThreadSafe in configuration if accessing from multiple threads
    /// - Async operations are always thread-safe regardless of configuration
    /// </remarks>
    public abstract class PersistentSingleton<T> : MonoBehaviour 
        where T : PersistentSingleton<T>
    {
        private static readonly AsyncReactiveProperty<T> _instanceSubject = 
            new AsyncReactiveProperty<T>(null);
            
        private static readonly AsyncReactiveProperty<bool> _readySubject = 
            new AsyncReactiveProperty<bool>(false);
            
        private static readonly CancellationTokenSource _destroyCts = 
            new CancellationTokenSource();
            
        private static readonly PersistentSingletonConfig _config;
        private static readonly object _lock = new object();
        private static bool _isQuitting;

        // Cache the configuration
        static PersistentSingleton()
        {
            _config = typeof(T).GetCustomAttribute<PersistentSingletonConfig>() 
                ?? new PersistentSingletonConfig();
        }

        /// <summary>
        /// Access the singleton instance synchronously.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: 
        /// 1. If using async initialization, use WaitForReadyAsync() instead
        /// 2. Returns null during application quit
        /// 3. Thread-safe only if configured with ThreadSafe = true
        /// </remarks>
        /// <example>
        /// <code>
        /// // Synchronous usage
        /// public class SoundManager : PersistentSingleton&lt;SoundManager&gt;
        /// {
        ///     public void PlaySound() { }
        /// }
        /// 
        /// // Client code
        /// SoundManager.Instance.PlaySound();
        /// </code>
        /// </example>
        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit.");
                    return null;
                }

                if (_instanceSubject.Value != null)
                    return _instanceSubject.Value;

                if (!_config.ThreadSafe)
                {
                    return CreateInstance();
                }

                lock (_lock)
                {
                    return CreateInstance();
                }
            }
        }

        /// <summary>
        /// Indicates if the singleton instance has been created.
        /// Does not guarantee the instance is fully initialized.
        /// </summary>
        public static bool IsInitialized => _instanceSubject.Value != null;

        /// <summary>
        /// Indicates if the singleton is fully initialized and ready for use.
        /// </summary>
        public static bool IsReady => _readySubject.Value;

        /// <summary>
        /// Asynchronously waits for the singleton instance to be created.
        /// Does not guarantee the instance is fully initialized.
        /// </summary>
        /// <param name="cancellation">Optional cancellation token</param>
        /// <returns>The singleton instance</returns>
        /// <exception cref="SingletonDestroyedException">Thrown if the singleton is destroyed</exception>
        public static async UniTask<T> WaitForInstanceAsync(
            CancellationToken cancellation = default)
        {
            if (_isQuitting)
                throw new SingletonDestroyedException(typeof(T));
        
            if (_instanceSubject.Value != null)
                return _instanceSubject.Value;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellation, _destroyCts.Token);
            
                await _instanceSubject.FirstAsync(x => x != null, cts.Token);
                return _instanceSubject.Value;
            }
            catch (OperationCanceledException) when (_destroyCts.IsCancellationRequested)
            {
                throw new SingletonDestroyedException(typeof(T));
            }
        }



        /// <summary>
        /// Asynchronously waits for the singleton to be fully initialized and ready.
        /// Use this when the singleton requires async initialization.
        /// </summary>
        /// <param name="cancellation">Optional cancellation token</param>
        /// <returns>The initialized singleton instance</returns>
        /// <exception cref="SingletonDestroyedException">Thrown if the singleton is destroyed</exception>
        /// <exception cref="OperationCanceledException">Thrown if initialization times out</exception>
        public static async UniTask<T> WaitForReadyAsync(
            CancellationToken cancellation = default)
        {
            if (_isQuitting)
                throw new SingletonDestroyedException(typeof(T));
        
            if (_instanceSubject.Value != null && _readySubject.Value)
                return _instanceSubject.Value;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellation, _destroyCts.Token);
            
                if (_instanceSubject.Value == null)
                    await _instanceSubject.FirstAsync(x => x != null, cts.Token);
            
                if (!_readySubject.Value)
                    await _readySubject.FirstAsync(x => x, cts.Token);
            
                return _instanceSubject.Value;
            }
            catch (OperationCanceledException) when (_destroyCts.IsCancellationRequested)
            {
                throw new SingletonDestroyedException(typeof(T));
            }
        }
        
        
        /// <summary>
        /// Creates or returns the singleton instance.
        /// </summary>
        private static T CreateInstance()
        {
            if (_instanceSubject.Value != null)
                return _instanceSubject.Value;

            var objects = FindObjectsOfType<T>();
            if (objects.Length > 0)
            {
                if (objects.Length > 1)
                {
                    Debug.LogWarning($"[Singleton] Multiple instances of {typeof(T)} found. Using the first one.");
                    for (int i = 1; i < objects.Length; i++)
                    {
                        Destroy(objects[i].gameObject);
                    }
                }
                _instanceSubject.Value = objects[0];
            }
            else
            {
                var go = new GameObject($"#{typeof(T).Name}");
                _instanceSubject.Value = go.AddComponent<T>();
            }

            DontDestroyOnLoad(_instanceSubject.Value.gameObject);
            return _instanceSubject.Value;
        }

        /// <summary>
        /// Override this method to implement async initialization logic.
        /// </summary>
        /// <returns>Initialization task</returns>
        /// <remarks>
        /// This method is called:
        /// - Immediately after creation if InitializationTiming.Immediate
        /// - On first access if InitializationTiming.Lazy
        /// </remarks>
        protected virtual async UniTask OnInitializeAsync()
        {
            await UniTask.CompletedTask;
        }

        private async UniTask InitializeInternalAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(_config.InitTimeout));

                await OnInitializeAsync().AttachExternalCancellation(cts.Token);
                _readySubject.Value = true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogError($"[Singleton] Initialization of {typeof(T)} timed out after {_config.InitTimeout} seconds.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Singleton] Failed to initialize {typeof(T)}: {ex}");
                throw;
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (_config.AutoInitOnStartup)
            {
                var _ = Instance; // This will trigger creation and initialization if needed
            }
        }

        /// <summary>
        /// Unity's Awake callback. Handles singleton instance setup.
        /// </summary>
        protected void Awake()
        {
            if (_instanceSubject.Value == null)
            {
                _instanceSubject.Value = (T)this;
                DontDestroyOnLoad(gameObject);

                if (_config.InitTiming == InitializationTiming.Immediate)
                {
                    InitializeInternalAsync().Forget();
                }
            }
            else if (_instanceSubject.Value != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity's OnDestroy callback. Handles cleanup.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instanceSubject.Value == this)
            {
                _destroyCts.Cancel();
                _instanceSubject.Value = null;
                _readySubject.Value = false;
            }
        }

        /// <summary>
        /// Unity's OnApplicationQuit callback. Handles quit state.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
