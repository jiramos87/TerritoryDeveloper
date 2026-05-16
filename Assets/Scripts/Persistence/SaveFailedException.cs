using System;

namespace Territory.Persistence
{
    /// <summary>Thrown by SaveCoordinator when a paired write fails mid-stream and rollback completes.</summary>
    public sealed class SaveFailedException : Exception
    {
        public SaveFailedException(string message) : base(message) { }
        public SaveFailedException(string message, Exception inner) : base(message, inner) { }
    }
}
