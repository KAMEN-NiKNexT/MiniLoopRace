using UnityEngine;

namespace MiniRace
{
    [CreateAssetMenu(fileName = "NewCarSettings", menuName = "MiniRace/Cars/Car Settings", order = 1)]
    public class CarSettings : ScriptableObject
    {
        #region --- Members ---

        [Header("Speed Settings")]

        [SerializeField] private int _maxSpeed;
        [SerializeField] private int _maxReverseSpeed;
        [SerializeField] private int _accelerationMultiplier;
        [SerializeField] private int _decelerationMultiplier;
        [SerializeField] private int _maxSteeringAngle;
        [SerializeField] private float _steeringSpeed;
        [SerializeField] private int _brakeForce;
        [SerializeField] private int _handbrakeDriftMultiplier;
        [SerializeField] private Vector3 _bodyMassCenter;

        #endregion

        #region --- Properties ---

        public int MaxSpeed { get => _maxSpeed; }
        public int MaxReverseSpeed { get => _maxReverseSpeed; }
        public int AccelerationMultiplier { get => _accelerationMultiplier; }
        public int DecelerationMultiplier { get => _decelerationMultiplier; }
        public int MaxSteeringAngle { get => _maxSteeringAngle; }
        public float SteeringSpeed { get => _steeringSpeed; }
        public int BrakeForce { get => _brakeForce; }
        public int HandbrakeDriftMultiplier { get => _handbrakeDriftMultiplier; }
        public Vector3 BodyMassCenter { get => _bodyMassCenter; }

        #endregion
    }
}