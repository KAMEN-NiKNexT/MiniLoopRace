using UnityEngine;

namespace MiniRace.Game
{
    public interface ICarPositionTracker
    {
        #region --- Properties ---

        public int CurrentLap { get; }
        public int CurrentCheckpointIndex { get; }
        public float DistanceToNextCheckpoint { get; }

        #endregion
    }
}