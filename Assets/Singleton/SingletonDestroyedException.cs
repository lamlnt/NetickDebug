using System;

namespace Core.Singleton
{
    /// <summary>
    /// Exception thrown when trying to access a destroyed singleton.
    /// </summary>
    public class SingletonDestroyedException : Exception
    {
        public SingletonDestroyedException(Type type) 
            : base($"Singleton of type {type.Name} was destroyed") { }
    }
}