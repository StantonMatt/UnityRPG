using UnityEngine;
using RPG.Stats;

namespace RPG.Combat
{
    /// <summary>
    /// Marks a GameObject as attackable.
    /// Requires Health component to receive damage.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class CombatTarget : MonoBehaviour
    {
        // This is a marker component
        // The Health component handles the actual damage/death logic
    }
}