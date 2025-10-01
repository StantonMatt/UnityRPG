namespace RPG.Core
{
    /// <summary>
    /// Interface for any action that can be scheduled and cancelled.
    /// Implemented by systems like Fighter, Mover, or any future action-based component.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Called when this action needs to be cancelled (e.g., when another action starts).
        /// </summary>
        void Cancel();
    }
}
