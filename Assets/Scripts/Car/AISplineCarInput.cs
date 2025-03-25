using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace MiniRace
{
    public class AISplineCarInput : MonoBehaviour, ICarInput
    {
        #region --- Members ---

        [Header("Spline Settings")]
        [SerializeField] private SplineContainer _pathSpline;
        [SerializeField] private float _maxRandomSpeedVariation = 0.2f;
        [SerializeField] private float _waypointReachedDistance = 3f;

        [SerializeField] private float _collisionDetectionDistance = 6f;
        [SerializeField] private float _avoidanceStrength = 0.7f;
        [SerializeField] private LayerMask _carLayerMask;

        [SerializeField] private float _optimalRacingLineOffset = 3f;

        [Header("Advanced Settings")]
        [SerializeField] private float _minSpeedOnTurn = 0.6f;
        [SerializeField] private float _maxBrakingAngle = 120f;

        private RaceCarController _carController;
        private float _steeringInput;
        private float _throttleInput;
        private bool _isHandbrakeActive;
        private float _currentSpeedVariation;
        private List<Collider> _nearbyColliders = new List<Collider>();
        private float _lastSteeringInput;

        #endregion

        #region --- Events ---

        public event Action OnInputUpdated;

        #endregion

        #region --- Properties ---

        public float ThrottleInput { get => _throttleInput; }
        public float SteeringInput { get => _steeringInput; }
        public bool IsHandbrakeActive { get => _isHandbrakeActive; }

        #endregion

        #region --- Mono Override Methods ---

        private void Awake()
        {
            Initialize();
        }
        private void Update()
        {
            UpdateInput();
        }

        private void OnCollisionEnter(Collision collision)
        {
            //if (((1 << collision.gameObject.layer) & _carLayerMask) != 0)
            //{
            //    HandleCarCollision(collision);
            //}
        }

        private void OnCollisionStay(Collision collision)
        {
            //// Продолжительный контакт с другими машинами
            //if (((1 << collision.gameObject.layer) & _carLayerMask) != 0 && _aggressionFactor > 0.3f)
            //{
            //    ApplyContinuousCollisionResponse(collision);
            //}
        }

        #endregion

        [Serializable]
        public class RoadSegment
        {
            public Vector3 StartPoint;
            public Vector3 EndPoint;
            public Vector3 Tangent1;
            public Vector3 Tangent2;
            public float Angle;

            public RoadSegment(float3 pos1, float3 pos2, float3 tan1, float3 tan2)
            {
                StartPoint = pos1;
                EndPoint = pos2;
                Tangent1 = new Vector3(tan1.x, tan1.y, tan1.z);
                Tangent2 = new Vector3(tan2.x, tan2.y, tan2.z);
                Angle = Vector3.Angle(Tangent1, Tangent2);
            }

        }

        #region --- ICarInput Implementation ---

        public void UpdateInput()
        {
            //UpdateRacingLine();
            UpdateTargetPoints();
            CheckForNearbyObstacles();

            CalculateSteeringInput();
            CalculateThrottleAndBrakeInput();

            //if (_isRecovering)
            //{
            //    PerformRecovery();
            //}
            //else
            //{
            //    CalculateSteeringInput();
            //    CalculateThrottleAndBrakeInput();
            //
            //    if (!_isPerformingAggressiveManeuver)
            //    {
            //        PerformAggressiveManeuversIfNeeded();
            //    }
            //
            //    ApplyRealisticInputLimitations();
            //}
            //
            //DetectOffTrackRecovery();
            OnInputUpdated?.Invoke();
        }

        #endregion

        #region --- Control Methods ---

        private void Initialize()
        {
            _carController = GetComponent<RaceCarController>();
            _currentSpeedVariation = UnityEngine.Random.Range(-_maxRandomSpeedVariation, _maxRandomSpeedVariation);

            CreateRoadSegments();
        }
        private void UpdateRacingLine()
        {
            //float a = UnityEngine.Random.value;
            //if (a < 0.005f)
            //{
            //    Debug.LogError(a);
            //    _targetPathOffset = Mathf.Lerp(_targetPathOffset, UnityEngine.Random.Range(-_optimalRacingLineOffset, _optimalRacingLineOffset), 0.1f);
            //}
        }
        public List<RoadSegment> segs = new List<RoadSegment>();
        private int _currentSegmentIndex;
        private void CreateRoadSegments()
        {
            for (int i = 0; i < 20; i++)
            {
                float3 position, tangent, up;
                _pathSpline.Evaluate(i / (20f + 1f), out position, out tangent, out up);

                float3 position2, tangent2, up2;
                _pathSpline.Evaluate((i + 1) / (20f + 1f), out position2, out tangent2, out up2);

                segs.Add(new RoadSegment(position, position2, tangent, tangent2));
                //Debug.DrawLine(position, position2, new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value), 50);
            }
            _currentSegmentIndex = 0;
        }

        private void UpdateTargetPoints()
        {
            int nextValue = _currentSegmentIndex + 1;
            if (nextValue > segs.Count - 1) nextValue = 0;

            if (Vector3.Distance(transform.position, segs[_currentSegmentIndex].EndPoint) < _waypointReachedDistance)
            {
                _currentSegmentIndex = nextValue;
            }
            else if (Vector3.Distance(transform.position, segs[nextValue].EndPoint) < _waypointReachedDistance)
            {
                _currentSegmentIndex = nextValue;
            }
        }

        private void CheckForNearbyObstacles()
        {
            _nearbyColliders.Clear();
            Collider[] colliders = Physics.OverlapSphere(transform.position, _collisionDetectionDistance, _carLayerMask);

            foreach (var collider in colliders)
            {
                if (collider.transform != transform)
                {
                    _nearbyColliders.Add(collider);
                }
            }
        }

        private void CalculateSteeringInput()
        {
            // Направление до конца текущего сектора
            Vector3 directionToSectorEnd = segs[_currentSegmentIndex].EndPoint - transform.position;
            float distanceToEnd = directionToSectorEnd.magnitude;
            Vector3 localDirectionToEnd = transform.InverseTransformDirection(directionToSectorEnd);

            // Получаем углы поворота в текущем и следующем секторах
            float currentSectorAngle = segs[_currentSegmentIndex].Angle;
            int nextSectorIndex = (_currentSegmentIndex + 1) % segs.Count;
            float nextSectorAngle = segs[nextSectorIndex].Angle;

            // Рассчитываем угол поворота относительно машины
            float angleToEnd = Mathf.Atan2(localDirectionToEnd.x, localDirectionToEnd.z) * Mathf.Rad2Deg;

            // Влияние следующего сектора увеличивается при приближении к концу текущего
            float blendFactor = Mathf.Clamp01(1 - (distanceToEnd / (_waypointReachedDistance * 5)));

            // Учитываем скорость - чем быстрее едем, тем сильнее поворачиваем
            float speedInfluence = Mathf.Clamp01(_carController.CurrentSpeed / 30f);

            // Смешиваем углы текущего и следующего секторов
            float targetAngle = Mathf.Lerp(currentSectorAngle, nextSectorAngle, blendFactor);

            // Применяем финальный поворот с учетом скорости
            float steeringMultiplier = Mathf.Lerp(0.5f, 1.5f, speedInfluence);
            float rawSteeringAmount = Mathf.Clamp(angleToEnd * steeringMultiplier / 45f, -1f, 1f);

            // Визуальная отладка
            //Debug.DrawRay(transform.position, directionToSectorEnd, Color.red, 0.1f);
            //Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue, 0.1f);

            // Применяем избегание препятствий и сглаживание поворота
            float avoidanceModifier = 0;
            if (rawSteeringAmount >= -0.3f && rawSteeringAmount <= 0.3f) avoidanceModifier = CalculateAvoidanceModifier();

            rawSteeringAmount = Mathf.Clamp(rawSteeringAmount + avoidanceModifier, -1f, 1f);
            _steeringInput = Mathf.Lerp(_lastSteeringInput, rawSteeringAmount, 0.9f);
            _lastSteeringInput = _steeringInput;
        }
        private float _val;
        private void CalculateThrottleAndBrakeInput()
        {
            _throttleInput = 1 - Mathf.Abs(_steeringInput);
        }


        private float CalculateAvoidanceModifier()
        {
            float avoidanceModifier = 0;

            // Учет препятствий
            foreach (var collider in _nearbyColliders)
            {
                Vector3 directionToObstacle = collider.transform.position - transform.position;
                float distance = directionToObstacle.magnitude;

                // Игнорируем машины позади
                if (Vector3.Dot(transform.forward, directionToObstacle) < 0)
                    continue;

                Vector3 localObstacleDir = transform.InverseTransformDirection(directionToObstacle);
                float side = Mathf.Sign(localObstacleDir.x);

                float proximityFactor = 1 - Mathf.Clamp01(distance / _collisionDetectionDistance);
                float forwardProximityScale = Mathf.Clamp01(localObstacleDir.z); // Сильнее реагируем на близкие по Z препятствия
                float strength = _avoidanceStrength * proximityFactor * proximityFactor * forwardProximityScale;

                avoidanceModifier -= side * strength;
            }
            return Mathf.Clamp(avoidanceModifier, -1f, 1f);
        }

        #endregion
    }
}