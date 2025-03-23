using UnityEngine;

namespace MiniRace
{
    [CreateAssetMenu(fileName = "NewCarSettings", menuName = "MiniRace/Cars/Car Settings", order = 1)]
    public class CarSettings : ScriptableObject
    {
        #region --- Members ---

        [Header("Speed Settings")]

        [SerializeField][Range(20, 3000)] private int _maxSpeed;
        [SerializeField][Range(10, 120)] private int _maxReverseSpeed;
        [SerializeField][Range(1, 100000)] private int _accelerationMultiplier;
        [SerializeField][Range(1, 10)] private int _decelerationMultiplier;
        [SerializeField][Range(5, 90)] private int _maxSteeringAngle;
        [SerializeField][Range(0.1f, 1f)] private float _steeringSpeed;
        [SerializeField][Range(100, 600)] private int _brakeForce;
        [SerializeField][Range(1, 10)] private int _handbrakeDriftMultiplier;
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