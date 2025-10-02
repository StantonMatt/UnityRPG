using UnityEngine;
using RPG.Core;

namespace RPG.Combat.Feedback
{
    /// <summary>
    /// Plays weapon swing sounds and handles weapon trail VFX.
    /// Listens to CombatEvents and plays sounds when this character attacks.
    /// Add this component to any character that should make swing sounds.
    /// </summary>
    [RequireComponent(typeof(Fighter))]
    public class WeaponFeedback : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("If not assigned, will create one automatically")]
        [SerializeField] private AudioSource audioSource;

        [Header("Visual Effects")]
        [Tooltip("Weapon trail renderer (optional - assign manually to weapon model)")]
        [SerializeField] private TrailRenderer weaponTrail;

        private void Awake()
        {
            // Create AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 20f;
            }

            // Disable trail by default
            if (weaponTrail != null)
            {
                weaponTrail.emitting = false;
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnAttackStarted += HandleAttackStarted;
        }

        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= HandleAttackStarted;
        }

        private void HandleAttackStarted(CombatEvents.AttackStartedEvent e)
        {
            // Only play sound if WE are attacking
            if (e.Attacker != gameObject) return;

            WeaponConfig weapon = e.Weapon;
            if (weapon == null)
            {
                GameDebug.LogWarning($"[WeaponFeedback] {gameObject.name} attacking but weapon is null",
                    config => config.logWeaponSounds, this);
                return;
            }

            // Play swing sound
            AudioClip swingSound = weapon.GetRandomSwingSound();
            if (swingSound != null)
            {
                audioSource.PlayOneShot(swingSound, weapon.swingVolume);
                GameDebug.Log($"[WeaponFeedback] {gameObject.name} playing swing sound: {swingSound.name}",
                    config => config.logWeaponSounds, this);
            }
            else
            {
                GameDebug.Log($"[WeaponFeedback] {gameObject.name} attacking but weapon has no swing sounds",
                    config => config.logWeaponSounds, this);
            }

            // Enable weapon trail if configured
            if (weaponTrail != null && weapon.enableWeaponTrail)
            {
                weaponTrail.emitting = true;
                weaponTrail.startColor = weapon.trailColor;
                weaponTrail.endColor = new Color(weapon.trailColor.r, weapon.trailColor.g, weapon.trailColor.b, 0f);

                // Disable trail after animation completes
                Invoke(nameof(DisableTrail), weapon.animationDuration);

                GameDebug.Log($"[WeaponFeedback] {gameObject.name} enabled weapon trail",
                    config => config.logWeaponSounds, this);
            }
        }

        private void DisableTrail()
        {
            if (weaponTrail != null)
            {
                weaponTrail.emitting = false;
                GameDebug.Log($"[WeaponFeedback] {gameObject.name} disabled weapon trail",
                    config => config.logWeaponSounds, this);
            }
        }
    }
}
