using System.Collections.Generic;
using MiniRace.Game;
using UnityEngine;

namespace MiniRace.Control
{
    public class RacePositionManager : SingletonComponent<RacePositionManager>
    {
        #region --- Members ---

        private List<ICarPositionTracker> _carTrackers = new List<ICarPositionTracker>();

        public void RegisterCar(ICarPositionTracker carTracker)
        {
            _carTrackers.Add(carTracker);
        }

        private void Update()
        {
            _carTrackers.Sort((a, b) =>
            {
                int lapComparison = b.CurrentLap.CompareTo(a.CurrentLap);
                if (lapComparison != 0) return lapComparison;

                int checkpointComparison = b.CurrentCheckpointIndex.CompareTo(a.CurrentCheckpointIndex);
                if (checkpointComparison != 0) return checkpointComparison;

                return a.DistanceToNextCheckpoint.CompareTo(b.DistanceToNextCheckpoint);
            });
        }
        #endregion
    }
}