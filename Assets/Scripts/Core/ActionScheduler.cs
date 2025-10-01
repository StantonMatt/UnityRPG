using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Manages action scheduling to prevent conflicting actions.
    /// Acts as a mutual dependency for Combat and Movement systems to avoid circular dependencies.
    /// When one action starts (e.g., attacking), it can cancel other actions (e.g., moving).
    /// </summary>
    public class ActionScheduler : MonoBehaviour
    {
        private IAction currentAction;

        /// <summary>
        /// Start a new action, automatically cancelling any previous action.
        /// </summary>
        public void StartAction(IAction action)
        {
            if (currentAction == action) return;

            if (currentAction != null)
            {
                currentAction.Cancel();
            }

            currentAction = action;
        }

        /// <summary>
        /// Cancel the current action if it matches the provided action.
        /// </summary>
        public void CancelCurrentAction()
        {
            StartAction(null);
        }
    }
}
