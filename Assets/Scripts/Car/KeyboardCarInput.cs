using UnityEngine;

namespace MiniRace
{
    public class KeyboardCarInput : MonoBehaviour, ICarInput
    {
        #region --- Properties ---

        public float ThrottleInput { get; private set; }
        public float SteeringInput { get; private set; }
        public bool IsHandbrakeActive { get; private set; }

        #endregion

        #region --- Unity Methods ---

        private void Update()
        {
            UpdateInput();
        }

        #endregion

        #region --- Public Methods ---

        public void UpdateInput()
        {
            float throttle = 0f;
            if (Input.GetKey(KeyCode.W)) throttle = 1f;
            else if (Input.GetKey(KeyCode.S)) throttle = -1f;
            ThrottleInput = throttle;

            float steering = 0f;
            if (Input.GetKey(KeyCode.A)) steering = -1f;
            else if (Input.GetKey(KeyCode.D)) steering = 1f;
            SteeringInput = steering;

            IsHandbrakeActive = Input.GetKey(KeyCode.Space);
        }

        #endregion
    }
}