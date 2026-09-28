namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Thread-safe least-recently-used cache of query embeddings, keyed by embedding endpoint and a SHA-256 of the
    /// query text. Repeated questions, suggested prompts and multi-query retrieval reuse a vector instead of calling
    /// the embedding model again.
    /// </summary>
    public class QueryEmbeddingCache
    {
        #region Public-Members

        /// <summary>
        /// Maximum number of entries. Zero disables the cache.
        /// </summary>
        public int Capacity => _Capacity;

        /// <summary>
        /// Current number of entries.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_Lock) return _Map.Count;
            }
        }

        #endregion

        #region Private-Members

        private readonly int _Capacity;
        private readonly object _Lock = new object();
        private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, List<double>>>> _Map = new Dictionary<string, LinkedListNode<KeyValuePair<string, List<double>>>>(StringComparer.Ordinal);
        private readonly LinkedList<KeyValuePair<string, List<double>>> _Order = new LinkedList<KeyValuePair<string, List<double>>>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="capacity">Maximum number of entries; zero disables the cache.</param>
        public QueryEmbeddingCache(int capacity)
        {
            _Capacity = Math.Max(0, capacity);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the cache key for an endpoint and query.
        /// </summary>
        /// <param name="endpointId">Embedding endpoint identifier.</param>
        /// <param name="query">Query text.</param>
        /// <returns>Cache key.</returns>
        public static string BuildKey(string endpointId, string query)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(query ?? ""));
            return (endpointId ?? "") + "|" + Convert.ToHexString(hash);
        }

        /// <summary>
        /// Look up a cached embedding.
        /// </summary>
        /// <param name="key">Cache key from <see cref="BuildKey"/>.</param>
        /// <param name="embedding">A copy of the cached vector when found.</param>
        /// <returns>True when found.</returns>
        public bool TryGet(string key, out List<double> embedding)
        {
            embedding = null;
            if (_Capacity == 0 || key == null) return false;

            lock (_Lock)
            {
                if (!_Map.TryGetValue(key, out LinkedListNode<KeyValuePair<string, List<double>>> node)) return false;
                _Order.Remove(node);
                _Order.AddFirst(node);
                embedding = new List<double>(node.Value.Value);
                return true;
            }
        }

        /// <summary>
        /// Store an embedding, evicting the least recently used entry when full.
        /// </summary>
        /// <param name="key">Cache key from <see cref="BuildKey"/>.</param>
        /// <param name="embedding">Embedding vector; a copy is stored.</param>
        public void Set(string key, List<double> embedding)
        {
            if (_Capacity == 0 || key == null || embedding == null || embedding.Count == 0) return;

            lock (_Lock)
            {
                if (_Map.TryGetValue(key, out LinkedListNode<KeyValuePair<string, List<double>>> existing))
                {
                    _Order.Remove(existing);
                    _Map.Remove(key);
                }

                LinkedListNode<KeyValuePair<string, List<double>>> node = new LinkedListNode<KeyValuePair<string, List<double>>>(
                    new KeyValuePair<string, List<double>>(key, new List<double>(embedding)));
                _Order.AddFirst(node);
                _Map[key] = node;

                while (_Map.Count > _Capacity)
                {
                    LinkedListNode<KeyValuePair<string, List<double>>> last = _Order.Last;
                    _Order.RemoveLast();
                    _Map.Remove(last.Value.Key);
                }
            }
        }

        #endregion
    }
}
