using UnityEngine;
using System.Collections;
using RPG.Core;

namespace RPG.Combat.VFX
{
    /// <summary>
    /// Spawns particle effects when this character is hit.
    /// Uses object pooling for performance (no constant Instantiate/Destroy calls).
    /// Listens to combat events to determine when/where to spawn VFX.
    /// </summary>
    public class HitVFXSpawner : MonoBehaviour
    {
        [Header("VFX Settings")]
        [Tooltip("Particle system prefab to spawn on hit (if null, uses weapon's VFX)")]
        [SerializeField] private GameObject defaultHitVFXPrefab;

        [Tooltip("Offset from hit point (useful for adjusting VFX position)")]
        [SerializeField] private Vector3 hitPointOffset = Vector3.zero;

        [Tooltip("Scale multiplier for spawned VFX")]
        [Range(0.1f, 5f)]
        [SerializeField] private float vfxScale = 1f;

        [Header("Pooling")]
        [Tooltip("Pre-create this many VFX instances (improves first-hit performance)")]
        [Range(0, 10)]
        [SerializeField] private int poolInitialSize = 3;

        [Tooltip("Use global pool manager (recommended) or local pool")]
        [SerializeField] private bool useGlobalPool = true;

        [Header("Performance")]
        [Tooltip("Minimum time between VFX spawns (prevents spam and job leaks)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float minTimeBetweenSpawns = 0.1f;

        // Object pool (per-prefab)
        private ObjectPool<ParticleSystem> localVFXPool;
        private float lastSpawnTime = -999f;

        private void Awake()
        {
            // Create local pool if not using global
            if (!useGlobalPool && defaultHitVFXPrefab != null)
            {
                Transform poolParent = new GameObject("HitVFX_Pool").transform;
                poolParent.SetParent(transform);
                localVFXPool = new ObjectPool<ParticleSystem>(defaultHitVFXPrefab, poolParent, poolInitialSize);

                GameDebug.Log($"[HitVFXSpawner] Created local pool for {gameObject.name} with {poolInitialSize} instances",
                    config => config.logVFXSpawner, this);
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageDealt += HandleDamageDealt;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageDealt -= HandleDamageDealt;
        }

        private void HandleDamageDealt(CombatEvents.DamageDealtEvent e)
        {
            // Only spawn VFX if WE are the target (not the attacker)
            if (e.Target != gameObject) return;

            // Cooldown check (prevent rapid spawning that causes job leaks)
            if (Time.time - lastSpawnTime < minTimeBetweenSpawns)
            {
                GameDebug.Log($"[HitVFXSpawner] Skipping VFX spawn (cooldown)",
                    config => config.logVFXSpawner, this);
                return;
            }
            lastSpawnTime = Time.time;

            // Determine which VFX prefab to use
            GameObject vfxPrefab = defaultHitVFXPrefab;

            // TODO: Future - get VFX from weapon config
            // WeaponConfig weapon = e.Weapon;
            // if (weapon != null && weapon.hitVFXPrefab != null)
            // {
            //     vfxPrefab = weapon.hitVFXPrefab;
            // }

            if (vfxPrefab == null)
            {
                GameDebug.Log($"[HitVFXSpawner] No VFX prefab assigned for {gameObject.name}",
                    config => config.logVFXSpawner, this);
                return;
            }

            // Spawn VFX at hit point
            SpawnVFX(vfxPrefab, e.HitPoint, e.HitNormal);
        }

        private void SpawnVFX(GameObject vfxPrefab, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Use hit point if provided, otherwise use character center
            Vector3 spawnPosition = hitPoint != Vector3.zero
                ? hitPoint + hitPointOffset
                : transform.position + Vector3.up + hitPointOffset;

            // Orient VFX to face away from hit direction
            Quaternion spawnRotation = hitNormal != Vector3.zero
                ? Quaternion.LookRotation(hitNormal)
                : Quaternion.identity;

            // Get VFX from pool
            ParticleSystem vfx = GetFromPool(vfxPrefab);
            if (vfx == null) return;

            // Position and orient
            vfx.transform.position = spawnPosition;
            vfx.transform.rotation = spawnRotation;
            vfx.transform.localScale = Vector3.one * vfxScale;

            // Play particle system
            vfx.Play();

            GameDebug.Log($"[HitVFXSpawner] Spawned {vfxPrefab.name} at {spawnPosition} (hitPoint: {hitPoint}, offset: {hitPointOffset}) for {gameObject.name}",
                config => config.logVFXSpawner, this);

            // Debug visualization (only in editor)
            #if UNITY_EDITOR
            Debug.DrawLine(hitPoint, spawnPosition, Color.green, 2f);
            Debug.DrawRay(hitPoint, Vector3.up * 0.5f, Color.red, 2f);
            #endif

            // Return to pool when done
            StartCoroutine(ReturnToPoolWhenDone(vfx, vfxPrefab));
        }

        private ParticleSystem GetFromPool(GameObject prefab)
        {
            if (useGlobalPool)
            {
                // Use global pool manager
                ObjectPool<ParticleSystem> pool = PoolManager.Instance.GetPool<ParticleSystem>(prefab, poolInitialSize);
                return pool.Get();
            }
            else
            {
                // Use local pool
                if (localVFXPool == null)
                {
                    GameDebug.LogWarning($"[HitVFXSpawner] Local pool not initialized for {gameObject.name}!",
                        config => config.logVFXSpawner, this);
                    return null;
                }
                return localVFXPool.Get();
            }
        }

        private void ReturnToPool(ParticleSystem vfx, GameObject prefab)
        {
            if (useGlobalPool)
            {
                ObjectPool<ParticleSystem> pool = PoolManager.Instance.GetPool<ParticleSystem>(prefab);
                pool.Return(vfx);
            }
            else
            {
                localVFXPool?.Return(vfx);
            }
        }

        private IEnumerator ReturnToPoolWhenDone(ParticleSystem vfx, GameObject prefab)
        {
            if (vfx == null) yield break;

            // Wait for main particle duration
            float duration = vfx.main.duration + vfx.main.startLifetime.constantMax;
            yield return new WaitForSeconds(duration);

            // Make sure all particles are finished (with null check)
            while (vfx != null && vfx.IsAlive())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Return to pool (check if still valid)
            if (vfx != null)
            {
                ReturnToPool(vfx, prefab);

                GameDebug.Log($"[HitVFXSpawner] Returned {prefab.name} to pool",
                    config => config.logVFXSpawner, this);
            }
        }

        // Editor helper - visualize hit point offset
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up + hitPointOffset, 0.1f);
        }
    }
}
