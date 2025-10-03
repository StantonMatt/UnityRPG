using UnityEngine;
using System.Collections.Generic;

namespace RPG.Core
{
    /// <summary>
    /// Generic object pooling system for reusing GameObjects.
    /// Prevents expensive Instantiate/Destroy calls by recycling objects.
    /// Commonly used for particles, projectiles, UI elements, etc.
    /// </summary>
    /// <typeparam name="T">Component type to pool (e.g., ParticleSystem, Projectile)</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> pool = new();
        private readonly Transform parent;
        private readonly GameObject prefab;
        private int totalCreated = 0;

        /// <summary>
        /// Create a new object pool.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate (must have component T)</param>
        /// <param name="parent">Parent transform for pooled objects (keeps hierarchy clean)</param>
        /// <param name="initialSize">Number of objects to pre-create</param>
        public ObjectPool(GameObject prefab, Transform parent = null, int initialSize = 0)
        {
            this.prefab = prefab;
            this.parent = parent;

            // Pre-warm pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// Get an object from the pool (or create new if pool is empty).
        /// </summary>
        public T Get()
        {
            T item;

            if (pool.Count > 0)
            {
                // Reuse existing object
                item = pool.Dequeue();
                item.gameObject.SetActive(true);

                GameDebug.Log($"[ObjectPool<{typeof(T).Name}>] Reused from pool. Pool size: {pool.Count}",
                    config => config.logObjectPool);
            }
            else
            {
                // Create new object
                item = CreateNewObject();
                item.gameObject.SetActive(true);

                GameDebug.Log($"[ObjectPool<{typeof(T).Name}>] Created new (pool empty). Total created: {totalCreated}",
                    config => config.logObjectPool);
            }

            return item;
        }

        /// <summary>
        /// Get an object at a specific position and rotation.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T item = Get();
            item.transform.position = position;
            item.transform.rotation = rotation;
            return item;
        }

        /// <summary>
        /// Return an object to the pool for reuse.
        /// </summary>
        public void Return(T item)
        {
            if (item == null)
            {
                GameDebug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Attempted to return null object!",
                    config => config.logObjectPool);
                return;
            }

            item.gameObject.SetActive(false);
            pool.Enqueue(item);

            GameDebug.Log($"[ObjectPool<{typeof(T).Name}>] Returned to pool. Pool size: {pool.Count}",
                config => config.logObjectPool);
        }

        /// <summary>
        /// Get current pool size (available objects).
        /// </summary>
        public int AvailableCount => pool.Count;

        /// <summary>
        /// Get total number of objects created (both active and pooled).
        /// </summary>
        public int TotalCreated => totalCreated;

        private T CreateNewObject()
        {
            GameObject obj = Object.Instantiate(prefab, parent);
            obj.SetActive(false); // Start inactive
            totalCreated++;

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                GameDebug.LogError($"[ObjectPool<{typeof(T).Name}>] Prefab '{prefab.name}' does not have component {typeof(T).Name}!");
            }

            return component;
        }
    }

    /// <summary>
    /// MonoBehaviour wrapper for managing multiple pools.
    /// Attach to a GameObject to manage all pooled objects in your scene.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;
        public static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("PoolManager");
                    instance = obj.AddComponent<PoolManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        private readonly Dictionary<string, object> pools = new();

        /// <summary>
        /// Get or create a pool for a specific prefab.
        /// </summary>
        public ObjectPool<T> GetPool<T>(GameObject prefab, int initialSize = 0) where T : Component
        {
            string key = $"{typeof(T).Name}_{prefab.name}";

            if (pools.TryGetValue(key, out object pool))
            {
                return (ObjectPool<T>)pool;
            }

            // Create new pool
            Transform poolParent = new GameObject($"Pool_{key}").transform;
            poolParent.SetParent(transform);

            ObjectPool<T> newPool = new ObjectPool<T>(prefab, poolParent, initialSize);
            pools[key] = newPool;

            GameDebug.Log($"[PoolManager] Created new pool: {key}",
                config => config.logObjectPool);

            return newPool;
        }

        /// <summary>
        /// Clear all pools (useful for scene transitions).
        /// </summary>
        public void ClearAllPools()
        {
            pools.Clear();

            // Destroy all pool parent objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            GameDebug.Log($"[PoolManager] Cleared all pools",
                config => config.logObjectPool);
        }
    }
}
