using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Singleton
{
    /// <summary>
    /// Defines when the singleton should initialize its resources.
    /// </summary>
    public enum InitializationTiming
    {
        /// <summary>
        /// Initialize only when first accessed.
        /// </summary>
        Lazy,

        /// <summary>
        /// Initialize immediately when the singleton is created.
        /// </summary>
        Immediate
    }
}
