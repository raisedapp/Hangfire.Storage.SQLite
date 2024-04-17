using System;
using System.Collections;
using System.Collections.Generic;

namespace Hangfire.Storage.SQLite
{
    public class PersistentJobQueueProviderCollection : IEnumerable<IPersistentJobQueueProvider>
    {
        private readonly List<IPersistentJobQueueProvider> _providers = new List<IPersistentJobQueueProvider>();

        private readonly Dictionary<string, IPersistentJobQueueProvider> _providersByQueue = new Dictionary<string, IPersistentJobQueueProvider>(StringComparer.OrdinalIgnoreCase);

        private readonly IPersistentJobQueueProvider _defaultProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PersistentJobQueueProviderCollection(IPersistentJobQueueProvider defaultProvider)
        {
            _defaultProvider = defaultProvider ?? throw new ArgumentNullException(nameof(defaultProvider));

            _providers.Add(_defaultProvider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="queues"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Add(IPersistentJobQueueProvider provider, IEnumerable<string> queues)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (queues == null)
                throw new ArgumentNullException(nameof(queues));

            _providers.Add(provider);

            foreach (var queue in queues)
            {
                _providersByQueue.Add(queue, provider);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public IPersistentJobQueueProvider GetProvider(string queue)
        {
            return _providersByQueue.TryGetValue(queue, out var value)
                ? value
                : _defaultProvider;
        }

        public IEnumerator<IPersistentJobQueueProvider> GetEnumerator()
        {
            return _providers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
