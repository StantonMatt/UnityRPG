using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// Configuration data for a weapon type.
    /// Create assets via: Create > RPG > Combat > Weapon Config
    /// Assign to Fighter component to use.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "RPG/Combat/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Base Stats")]
        [Tooltip("Damage dealt per hit")]
        [Min(0)]
        public float damage = 10f;

        [Tooltip("Attack range in units")]
        [Min(0.1f)]
        public float range = 2f;

        [Tooltip("Time between attacks (cooldown)")]
        [Min(0.1f)]
        public float attackCooldown = 1f;

        [Tooltip("Duration of attack animation (for rooting)")]
        [Min(0.1f)]
        public float animationDuration = 0.8f;

        [Header("Visual Feedback")]
        [Tooltip("Particle effect spawned at hit point (optional)")]
        public GameObject hitVFXPrefab;

        [Tooltip("Color to flash target when hit")]
        public Color hitFlashColor = Color.white;

        [Tooltip("How long the hit flash lasts")]
        [Range(0.01f, 0.5f)]
        public float hitFlashDuration = 0.1f;

        [Tooltip("Enable weapon trail during swing")]
        public bool enableWeaponTrail = true;

        [Tooltip("Color of weapon trail")]
        public Color trailColor = Color.white;

        [Header("Audio Feedback")]
        [Tooltip("Sounds played when swinging (random)")]
        public AudioClip[] swingSounds;

        [Tooltip("Sounds played on successful hit (random)")]
        public AudioClip[] hitSounds;

        [Tooltip("Volume of swing sounds")]
        [Range(0f, 1f)]
        public float swingVolume = 0.7f;

        [Tooltip("Volume of hit sounds")]
        [Range(0f, 1f)]
        public float hitVolume = 0.8f;

        [Header("Impact Feedback")]
        [Tooltip("Enable brief time freeze on hit")]
        public bool enableHitStop = true;

        [Tooltip("Duration of hit stop in seconds")]
        [Range(0f, 0.2f)]
        public float hitStopDuration = 0.05f;

        [Tooltip("Enable camera shake on hit")]
        public bool enableCameraShake = false;

        [Tooltip("Intensity of camera shake")]
        [Range(0f, 1f)]
        public float cameraShakeIntensity = 0.2f;

        [Tooltip("Duration of camera shake")]
        [Range(0.05f, 0.5f)]
        public float cameraShakeDuration = 0.2f;

        [Tooltip("Enable knockback on hit")]
        public bool enableKnockback = false;

        [Tooltip("Force applied for knockback")]
        [Min(0f)]
        public float knockbackForce = 5f;

        [Header("Animation")]
        [Tooltip("Enable hit reaction animation on target")]
        public bool enableHitReact = true;

        [Tooltip("Animator trigger name for hit reaction")]
        public string hitReactTrigger = "hitReact";

        // ===== Helper Methods =====

        /// <summary>
        /// Get a random swing sound from the array.
        /// </summary>
        public AudioClip GetRandomSwingSound()
        {
            if (swingSounds == null || swingSounds.Length == 0)
                return null;

            return swingSounds[Random.Range(0, swingSounds.Length)];
        }

        /// <summary>
        /// Get a random hit sound from the array.
        /// </summary>
        public AudioClip GetRandomHitSound()
        {
            if (hitSounds == null || hitSounds.Length == 0)
                return null;

            return hitSounds[Random.Range(0, hitSounds.Length)];
        }
    }
}
