using System;
using UnityEngine;
using RPG.Core;

// Required for C# 9 records in Unity
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace RPG.Combat
{
    /// <summary>
    /// Central event bus for all combat-related events.
    /// Allows feedback systems to react to damage, attacks, and deaths without tight coupling.
    /// </summary>
    public static class CombatEvents
    {
        // ===== Event Data Records =====

        /// <summary>
        /// Fired when damage is dealt (from attacker's perspective).
        /// Contains hit point/normal for VFX placement.
        /// </summary>
        public record DamageDealtEvent(
            GameObject Attacker,
            GameObject Target,
            float Damage,
            Vector3 HitPoint,
            Vector3 HitNormal
        );

        /// <summary>
        /// Fired when damage is taken (from target's perspective).
        /// Contains current/max health for UI updates.
        /// </summary>
        public record DamageTakenEvent(
            GameObject Target,
            float Damage,
            float CurrentHealth,
            float MaxHealth
        );

        /// <summary>
        /// Fired when an attack animation starts.
        /// Used for swing sounds, weapon trails, etc.
        /// </summary>
        public record AttackStartedEvent(
            GameObject Attacker,
            WeaponConfig Weapon
        );

        /// <summary>
        /// Fired when an attack successfully hits a target.
        /// Used for impact sounds, VFX, hitstop, camera shake, etc.
        /// </summary>
        public record AttackHitEvent(
            GameObject Attacker,
            GameObject Target,
            WeaponConfig Weapon,
            Vector3 HitPoint,
            Vector3 HitNormal
        );

        /// <summary>
        /// Fired when a character dies.
        /// </summary>
        public record DeathEvent(
            GameObject Target,
            GameObject Killer
        );

        // ===== Event Declarations =====

        public static event Action<DamageDealtEvent> OnDamageDealt;
        public static event Action<DamageTakenEvent> OnDamageTaken;
        public static event Action<AttackStartedEvent> OnAttackStarted;
        public static event Action<AttackHitEvent> OnAttackHit;
        public static event Action<DeathEvent> OnDeath;

        // ===== Event Raising Methods =====

        public static void RaiseDamageDealt(DamageDealtEvent e)
        {
            OnDamageDealt?.Invoke(e);

            GameDebug.Log($"[CombatEvents] DamageDealt: {e.Attacker?.name} → {e.Target?.name} ({e.Damage} damage)",
                config => config.logCombatEvents);
        }

        public static void RaiseDamageTaken(DamageTakenEvent e)
        {
            OnDamageTaken?.Invoke(e);

            GameDebug.Log($"[CombatEvents] DamageTaken: {e.Target?.name} ({e.CurrentHealth}/{e.MaxHealth} HP)",
                config => config.logCombatEvents);
        }

        public static void RaiseAttackStarted(AttackStartedEvent e)
        {
            OnAttackStarted?.Invoke(e);

            GameDebug.Log($"[CombatEvents] AttackStarted: {e.Attacker?.name} with {e.Weapon?.name}",
                config => config.logAttackEvents);
        }

        public static void RaiseAttackHit(AttackHitEvent e)
        {
            OnAttackHit?.Invoke(e);

            GameDebug.Log($"[CombatEvents] AttackHit: {e.Attacker?.name} → {e.Target?.name} at {e.HitPoint}",
                config => config.logAttackEvents);
        }

        public static void RaiseDeath(DeathEvent e)
        {
            OnDeath?.Invoke(e);

            GameDebug.Log($"[CombatEvents] Death: {e.Target?.name} killed by {e.Killer?.name}",
                config => config.logCombatEvents);
        }

        /// <summary>
        /// Clear all event subscriptions. Call on scene transitions if needed.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnDamageDealt = null;
            OnDamageTaken = null;
            OnAttackStarted = null;
            OnAttackHit = null;
            OnDeath = null;
        }
    }
}
