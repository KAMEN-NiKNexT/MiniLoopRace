using System;

namespace MiniRace
{
    public interface ICarInput
    {
        #region --- Events ---

        public event Action OnInputUpdated;

        #endregion

        #region --- Properties ---

        public float ThrottleInput { get; }
        public float SteeringInput { get; }
        public bool IsHandbrakeActive { get; }

        #endregion

        #region --- Methods ---

        public void UpdateInput();

        #endregion
    }
}