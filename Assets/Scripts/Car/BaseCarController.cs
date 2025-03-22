using UnityEngine;

namespace MiniRace
{
    public abstract class BaseCarController : MonoBehaviour
    {
        #region --- Properties ---

        public float CurrentSpeed { get; protected set; }
        public bool IsDrifting { get; protected set; }
        public bool IsTractionLocked { get; protected set; }

        #endregion

        #region --- Abstract Methods ---

        protected abstract void InitializeCar();
        public abstract void Accelerate(float throttleInput);
        public abstract void Reverse(float throttleInput);
        public abstract void Steer(float steeringInput);
        public abstract void ApplyHandbrake();
        public abstract void ReleaseHandbrake();
        public abstract void ReleaseThrottle();
        protected abstract void UpdateCarData();
        protected abstract void ApplyBrakes();
        protected abstract void DecelerateCar();

        #endregion
    }
}