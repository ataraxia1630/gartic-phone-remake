using System;
using System.Collections.Generic;

namespace InkEcho.Network.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, service))
                _services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                service = obj as T;
                return service != null;
            }
            service = null;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}
