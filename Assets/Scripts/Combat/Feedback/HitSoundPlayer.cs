using UnityEngine;
using RPG.Core;

namespace RPG.Combat.Feedback
{
    /// <summary>
    /// Plays impact sounds when this character is hit.
    /// Listens to CombatEvents and plays sounds from the attacker's WeaponConfig.
    /// Add this component to any character that should make a sound when damaged.
    /// </summary>
    public class HitSoundPlayer : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("If not assigned, will create one automatically")]
        [SerializeField] private AudioSource audioSource;

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
        }

        private void OnEnable()
        {
            CombatEvents.OnAttackHit += HandleAttackHit;
        }

        private void OnDisable()
        {
            CombatEvents.OnAttackHit -= HandleAttackHit;
        }

        private void HandleAttackHit(CombatEvents.AttackHitEvent e)
        {
            // Only play sound if WE are the target
            if (e.Target != gameObject) return;

            WeaponConfig weapon = e.Weapon;
            if (weapon == null)
            {
                GameDebug.LogWarning($"[HitSoundPlayer] {gameObject.name} was hit but weapon is null",
                    config => config.logHitSounds, this);
                return;
            }

            // Get random hit sound from weapon config
            AudioClip hitSound = weapon.GetRandomHitSound();
            if (hitSound != null)
            {
                audioSource.PlayOneShot(hitSound, weapon.hitVolume);
                GameDebug.Log($"[HitSoundPlayer] {gameObject.name} playing hit sound: {hitSound.name}",
                    config => config.logHitSounds, this);
            }
            else
            {
                GameDebug.Log($"[HitSoundPlayer] {gameObject.name} hit but weapon has no hit sounds",
                    config => config.logHitSounds, this);
            }
        }
    }
}
