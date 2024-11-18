using System;

namespace Core.Singleton
{
    /// <summary>
    /// Configuration for persistent singletons.
    /// </summary>
    /// <remarks>
    /// Use this attribute to configure the behavior of your singleton.
    /// If not specified, default values will be used.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PersistentSingletonConfig : Attribute
    {
        /// <summary>
        /// Determines if the singleton needs thread-safe access.
        /// Enable if accessing from multiple threads.
        /// </summary>
        public bool ThreadSafe { get; }

        /// <summary>
        /// When the singleton should initialize its resources.
        /// </summary>
        public InitializationTiming InitTiming { get; }

        /// <summary>
        /// Maximum time in seconds allowed for initialization.
        /// </summary>
        public float InitTimeout { get; }
        
        public bool AutoInitOnStartup { get; } 

        /// <summary>
        /// Configure a persistent singleton's behavior.
        /// </summary>
        /// <param name="threadSafe">Enable for multi-threaded access</param>
        /// <param name="timing">When to initialize the singleton</param>
        /// <param name="timeout">Maximum initialization time in seconds</param>
        public PersistentSingletonConfig(
            bool threadSafe = true,
            InitializationTiming timing = InitializationTiming.Lazy,
            float timeout = 5f,
            bool autoInitOnStartup = false)
        {
            ThreadSafe = threadSafe;
            InitTiming = timing;
            InitTimeout = timeout;
            AutoInitOnStartup = autoInitOnStartup;
        }
    }
}
