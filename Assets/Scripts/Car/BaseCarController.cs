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

        protected abstract void Initialize();
        protected abstract void Accelerate(float throttleInput);
        protected abstract void Reverse(float throttleInput);
        protected abstract void Steer(float steeringInput);
        protected abstract void ApplyHandbrake();
        protected abstract void ReleaseHandbrake();
        protected abstract void ReleaseThrottle();
        protected abstract void UpdateCarData();
        protected abstract void ApplyBrakes();
        protected abstract void DecelerateCar();

        #endregion
    }
}