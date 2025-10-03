using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Centralized debug configuration for all game systems.
    /// Create one instance via Assets > Create > RPG/Debug Config
    /// </summary>
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "RPG/Debug Config", order = 0)]
    public class DebugConfig : ScriptableObject
    {
        [Header("Combat System")]
        [Tooltip("Log damage events (dealt and taken)")]
        public bool logCombatEvents = false;

        [Tooltip("Log attack events (started and hit)")]
        public bool logAttackEvents = false;

        [Tooltip("Log Fighter state changes (target acquisition, attack triggers)")]
        public bool logFighterState = false;

        [Header("Audio System")]
        [Tooltip("Log when hit sounds play")]
        public bool logHitSounds = false;

        [Tooltip("Log when weapon swing sounds play")]
        public bool logWeaponSounds = false;

        [Header("Visual Feedback")]
        [Tooltip("Log hit flash initialization and triggers")]
        public bool logHitFlash = false;

        [Tooltip("Log hit react animation triggers")]
        public bool logHitReact = false;

        [Tooltip("Log knockback triggers and physics")]
        public bool logKnockback = false;

        [Header("Movement System")]
        [Tooltip("Log mover actions (MoveTo, Cancel, etc.)")]
        public bool logMoverActions = false;

        [Tooltip("Log pathfinding operations")]
        public bool logPathfinding = false;

        [Header("AI System")]
        [Tooltip("Log AI state changes and decisions")]
        public bool logAIController = false;

        [Tooltip("Log patrol behavior")]
        public bool logPatrolBehavior = false;

        [Header("Player Control")]
        [Tooltip("Log player input and actions")]
        public bool logPlayerController = false;

        [Header("Stats & Health")]
        [Tooltip("Log health changes and death")]
        public bool logHealthSystem = false;

        [Tooltip("Log stat modifications")]
        public bool logStatsSystem = false;

        [Header("VFX & Particles")]
        [Tooltip("Log VFX spawning and pooling")]
        public bool logVFXSpawner = false;

        [Tooltip("Log object pool operations (get/return)")]
        public bool logObjectPool = false;

        [Header("Camera & Time")]
        [Tooltip("Log camera shake triggers and intensity")]
        public bool logCameraShake = false;

        [Tooltip("Log time controller operations (hitstop, slow-mo)")]
        public bool logTimeController = false;

        [Header("General")]
        [Tooltip("Master toggle - disables ALL debug logs when off")]
        public bool enableDebugLogs = true;

        /// <summary>
        /// Singleton instance - will be loaded from Resources folder
        /// </summary>
        private static DebugConfig instance;
        public static DebugConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<DebugConfig>("DebugConfig");
                    if (instance == null)
                    {
                        Debug.LogWarning("DebugConfig not found in Resources folder! Create one at Assets/Resources/DebugConfig.asset");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Helper method to check if a specific debug category is enabled
        /// </summary>
        public static bool IsEnabled(System.Func<DebugConfig, bool> selector)
        {
            if (Instance == null) return false;
            if (!Instance.enableDebugLogs) return false;
            return selector(Instance);
        }
    }

    /// <summary>
    /// Static helper class for clean debug logging syntax
    /// </summary>
    public static class GameDebug
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string message, System.Func<DebugConfig, bool> selector, Object context = null)
        {
            if (DebugConfig.IsEnabled(selector))
            {
                Debug.Log(message, context);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message, System.Func<DebugConfig, bool> selector, Object context = null)
        {
            if (DebugConfig.IsEnabled(selector))
            {
                Debug.LogWarning(message, context);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(string message, Object context = null)
        {
            // Errors always log regardless of settings
            Debug.LogError(message, context);
        }
    }
}
