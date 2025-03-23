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
        [SerializeField] private float _speedFactor = 1f;
        [SerializeField] private float _maxRandomSpeedVariation = 0.2f;
        [SerializeField] private float _waypointReachedDistance = 3f;

        [Header("Racing Behavior")]
        [SerializeField] private float _baseLookAheadDistance = 5f;
        [SerializeField] private float _speedBasedLookAheadFactor = 0.1f;
        [SerializeField] private float _aggressionFactor = 0.5f;
        [SerializeField] private float _sideHitForce = 8f;
        [SerializeField] private float _frontHitForce = 12f;
        [SerializeField] private float _collisionDetectionDistance = 6f;
        [SerializeField] private float _avoidanceStrength = 0.7f;
        [SerializeField] private LayerMask _carLayerMask;

        [Header("Racing Line")]
        [SerializeField] private float _optimalRacingLineOffset = 3f;
        [SerializeField] private float _cornerCuttingFactor = 0.8f;
        [SerializeField] private float _targetPointAdvanceDistance = 8f; // Расстояние для перемещения target point вперед

        [Header("Advanced Settings")]
        [SerializeField] private float _minSpeedOnTurn = 0.6f;
        [SerializeField] private float _maxBrakingAngle = 120f;
        [SerializeField] private float _handbrakeOnSharpTurnThreshold = 60f;
        [SerializeField] private float _driftExitSteeringFactor = 0.5f;
        [SerializeField] private float _recoverySteeringStrength = 1.5f;

        private RaceCarController _carController;
        private Rigidbody _carRigidbody;
        private float _currentSteeringInput;
        private float _currentThrottleInput = 1f;
        private bool _isHandbrakeActive;
        private float _currentSpeedVariation;
        private Vector3 _targetPoint;
        private Vector3 _nextTargetPoint;
        private Vector3 _tangentDirection;
        private List<Collider> _nearbyColliders = new List<Collider>();
        private float _lastPathPercent = -1f;
        private float _timeUntilNextAggression;
        private float _targetPathOffset;
        private float _steeringDamping = 0.2f;
        private float _lastSteeringInput;
        private bool _isPerformingAggressiveManeuver;
        private float _recoveryTimer;
        private bool _isRecovering;
        private bool _isOffRacingLine;
        private bool _wasBraking;
        private float _currentSplinePercent;
        private float _nextSplinePercent;

        #endregion

        #region --- Events ---

        public event Action OnInputUpdated;

        #endregion

        #region --- Properties ---

        public float ThrottleInput { get => _currentThrottleInput; }
        public float SteeringInput { get => _currentSteeringInput; }
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

        #region --- ICarInput Implementation ---

        public void UpdateInput()
        {
            UpdateRacingLine();
            UpdateTargetPoints();
            CheckForNearbyObstacles();

            if (_isRecovering)
            {
                PerformRecovery();
            }
            else
            {
                CalculateSteeringInput();
                CalculateThrottleAndBrakeInput();

                if (!_isPerformingAggressiveManeuver)
                {
                    PerformAggressiveManeuversIfNeeded();
                }

                ApplyRealisticInputLimitations();
            }

            DetectOffTrackRecovery();
            OnInputUpdated?.Invoke();
        }

        #endregion

        #region --- Control Methods ---

        private void Initialize()
        {
            _carController = GetComponent<RaceCarController>();
            _carRigidbody = GetComponent<Rigidbody>();
            _currentSpeedVariation = UnityEngine.Random.Range(-_maxRandomSpeedVariation, _maxRandomSpeedVariation);
            _timeUntilNextAggression = UnityEngine.Random.Range(5f, 15f);
            _targetPathOffset = UnityEngine.Random.Range(-_optimalRacingLineOffset, _optimalRacingLineOffset);
            _currentSplinePercent = 0f;
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
        private void UpdateTargetPoints()
        {
            if (_targetPoint != Vector3.zero && Vector3.Distance(transform.position, _targetPoint) < _waypointReachedDistance)
            {
                _currentSplinePercent = _nextSplinePercent;
            }

            float3 position, tangent, up;
            _pathSpline.Evaluate(_currentSplinePercent, out position, out tangent, out up);
            _tangentDirection = tangent;

            Vector3 vectorPosition = new Vector3(position.x, position.y, position.z);
            Vector3 trackNormal = Vector3.Cross(tangent, Vector3.up).normalized;
            _targetPoint = vectorPosition + trackNormal * _targetPathOffset;

            float dynamicLookAhead = _baseLookAheadDistance + _carController.CurrentSpeed * _speedBasedLookAheadFactor;
            _nextSplinePercent = Mathf.Repeat(_currentSplinePercent + (dynamicLookAhead / _pathSpline.Spline.GetLength()), 1f);
            _pathSpline.Evaluate(_nextSplinePercent, out position, out tangent, out up);

            Vector3 currentDirection = _tangentDirection;
            Vector3 nextDirection = tangent;
            float cornerSharpness = Vector3.Angle(currentDirection, nextDirection);
            float cornerCutting = _cornerCuttingFactor * (cornerSharpness / 90f);

            trackNormal = Vector3.Cross(tangent, Vector3.up).normalized;
            Vector3 insideCornerOffset = trackNormal * _targetPathOffset;
            if (Vector3.Dot(trackNormal, transform.right) < 0)
            {
                insideCornerOffset *= 1 + cornerCutting;
            }

            _nextTargetPoint = vectorPosition + insideCornerOffset;
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
            Vector3 directionToTarget = _targetPoint - transform.position;
            Vector3 localDirection = transform.InverseTransformDirection(directionToTarget);
            float targetAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

            // Добавляем учет скорости - на высокой скорости поворачиваем плавнее
            float speedFactor = Mathf.Clamp01(_carController.CurrentSpeed / 50f);
            float steeringResponse = Mathf.Lerp(1.0f, 0.6f, speedFactor);

            // Если идет занос, учитываем боковую скорость при расчете рулежки
            bool isDrifting = _carController.IsDrifting;
            if (isDrifting)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(_carRigidbody.linearVelocity);
                float driftAngle = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;

                // Противоруление при заносе
                if (Mathf.Abs(driftAngle) > 5)
                {
                    float counterSteerFactor = _driftExitSteeringFactor * Mathf.Sign(driftAngle);
                    targetAngle *= (1 - Mathf.Abs(counterSteerFactor));
                    targetAngle -= driftAngle * counterSteerFactor;
                }
            }

            float avoidanceModifier = CalculateAvoidanceModifier();
            float rawSteeringAmount = Mathf.Clamp(targetAngle / 45f * steeringResponse + avoidanceModifier, -1f, 1f);

            // Плавное изменение руления для реалистичности
            _currentSteeringInput = Mathf.Lerp(_lastSteeringInput, rawSteeringAmount, 1 - _steeringDamping);
            _lastSteeringInput = _currentSteeringInput;
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

                // Более интенсивное избегание при приближении
                float proximityFactor = 1 - Mathf.Clamp01(distance / _collisionDetectionDistance);
                float forwardProximityScale = Mathf.Clamp01(localObstacleDir.z / 5f); // Сильнее реагируем на близкие по Z препятствия

                float strength = _avoidanceStrength * proximityFactor * proximityFactor * forwardProximityScale;

                // В зависимости от агрессивности и расположения машины иногда стремимся в сторону соперника
                if (UnityEngine.Random.value < _aggressionFactor * 0.1f && distance < _collisionDetectionDistance * 0.7f)
                {
                    strength *= -2.0f; // Направляемся на соперника для тарана
                    _isPerformingAggressiveManeuver = true;
                    _timeUntilNextAggression = 1.0f; // Короткая агрессивная фаза
                }
                else
                {
                    // Обычное избегание
                    avoidanceModifier -= side * strength;
                }
            }

            return Mathf.Clamp(avoidanceModifier, -1f, 1f);
        }

        private void CalculateThrottleAndBrakeInput()
        {
            // Определяем насколько крутой предстоящий поворот
            Vector3 currentForward = _tangentDirection;
            Vector3 nextForward = (_nextTargetPoint - _targetPoint).normalized;
            float turnAngle = Vector3.Angle(currentForward, nextForward);

            // Рассчитываем насколько нужно замедлиться в зависимости от крутизны поворота
            float turnFactor = 1f;
            if (turnAngle > 10)
            {
                turnFactor = Mathf.Clamp01(1f - (turnAngle / _maxBrakingAngle));
            }

            // Более интенсивное торможение на крутых поворотах в соответствии со скоростью
            bool shouldBrake = false;
            float speedAdjustedTurnAngle = turnAngle * (_carController.CurrentSpeed / 40f);

            if (speedAdjustedTurnAngle > 45f)
            {
                shouldBrake = true;
                // На очень крутых поворотах тормозим резче
                turnFactor *= 0.5f;
            }

            // Определяем базовую скорость с учетом поворота и случайных вариаций для реализма
            float targetThrottle = Mathf.Lerp(_minSpeedOnTurn, 1f, turnFactor) + _currentSpeedVariation;
            targetThrottle *= _speedFactor;

            // Плавно меняем дроссель для реализма
            if (shouldBrake && !_wasBraking)
            {
                // Резкое торможение в начале
                _currentThrottleInput = -0.5f;
                _wasBraking = true;
            }
            else if (shouldBrake)
            {
                // Продолжаем торможение, но плавно регулируем
                _currentThrottleInput = Mathf.Lerp(_currentThrottleInput, -0.8f, Time.deltaTime * 2f);
            }
            else
            {
                // Плавная регулировка дросселя
                _currentThrottleInput = Mathf.Lerp(_currentThrottleInput, targetThrottle, Time.deltaTime * 3f);
                _wasBraking = false;
            }

            // Управление ручным тормозом для дрифта на крутых поворотах
            _isHandbrakeActive = turnAngle > _handbrakeOnSharpTurnThreshold && _carController.CurrentSpeed > 30f;

            // На крутых поворотах на высокой скорости иногда используем дрифт даже если угол меньше порога
            if (!_isHandbrakeActive && turnAngle > _handbrakeOnSharpTurnThreshold * 0.7f &&
                _carController.CurrentSpeed > 45f && UnityEngine.Random.value < 0.3f)
            {
                _isHandbrakeActive = true;
            }
        }

        private void ApplyRealisticInputLimitations()
        {
            // Симуляция человеческих ограничений - нельзя одновременно полностью рулить и полностью газовать/тормозить
            float steeringMagnitude = Mathf.Abs(_currentSteeringInput);
            if (steeringMagnitude > 0.7f && Mathf.Abs(_currentThrottleInput) > 0.8f)
            {
                // При сильном рулении немного сбрасываем газ/тормоз
                _currentThrottleInput *= (1f - (steeringMagnitude - 0.7f));
            }

            // Небольшая случайная вариация входных данных для реализма
            if (UnityEngine.Random.value < 0.05f)
            {
                _currentSteeringInput += UnityEngine.Random.Range(-0.05f, 0.05f);
                _currentSteeringInput = Mathf.Clamp(_currentSteeringInput, -1f, 1f);

                _currentThrottleInput += UnityEngine.Random.Range(-0.03f, 0.03f);
                _currentThrottleInput = Mathf.Clamp(_currentThrottleInput, -1f, 1f);
            }
        }

        private void PerformAggressiveManeuversIfNeeded()
        {
            if (_isPerformingAggressiveManeuver)
            {
                _timeUntilNextAggression -= Time.deltaTime;
                if (_timeUntilNextAggression <= 0)
                {
                    _isPerformingAggressiveManeuver = false;
                    _timeUntilNextAggression = UnityEngine.Random.Range(5f, 15f);
                }
                return;
            }

            _timeUntilNextAggression -= Time.deltaTime;

            // Пропорционально агрессивности решаем, будем ли таранить соперников
            if (_timeUntilNextAggression <= 0 && _nearbyColliders.Count > 0 &&
                UnityEngine.Random.value < _aggressionFactor * _aggressionFactor)
            {
                Collider targetCar = GetBestTargetCar();
                if (targetCar != null)
                {
                    AttemptAggressiveManeuver(targetCar);
                }
            }
        }

        private void AttemptAggressiveManeuver(Collider targetCar)
        {
            Vector3 directionToCar = targetCar.transform.position - transform.position;
            Vector3 localDirToCar = transform.InverseTransformDirection(directionToCar);

            _isPerformingAggressiveManeuver = true;
            _timeUntilNextAggression = UnityEngine.Random.Range(3f, 8f);

            if (IsValidTargetForSideHit(localDirToCar))
            {
                // Боковой удар - подрезание
                float sideHitDirection = Mathf.Sign(localDirToCar.x);

                _currentSteeringInput = sideHitDirection * 0.8f;
                _currentThrottleInput = 1f;

                ApplyHitForce(targetCar, transform.right * sideHitDirection, _sideHitForce);
            }
            else if (IsValidTargetForRearHit(localDirToCar))
            {
                // Удар сзади - толкание
                _currentSteeringInput = Mathf.Clamp(localDirToCar.x * 2f, -1f, 1f);
                _currentThrottleInput = 1f;

                ApplyHitForce(targetCar, transform.forward, _frontHitForce);
            }
        }

        private void ApplyHitForce(Collider targetCollider, Vector3 direction, float force)
        {
            Rigidbody targetRb = targetCollider.GetComponentInParent<Rigidbody>();
            if (targetRb != null && _carRigidbody != null)
            {
                _carRigidbody.AddForce(direction * force, ForceMode.Impulse);
            }
        }

        private bool IsValidTargetForSideHit(Vector3 localDirToCar)
        {
            // Проверка возможности бокового удара (соперник сбоку и немного впереди)
            return Mathf.Abs(localDirToCar.x) > 0.5f &&
                   Mathf.Abs(localDirToCar.x) < 2.5f &&
                   localDirToCar.z > 0 &&
                   localDirToCar.z < 5f;
        }

        private bool IsValidTargetForRearHit(Vector3 localDirToCar)
        {
            // Проверка возможности удара сзади (соперник прямо перед нами)
            return Mathf.Abs(localDirToCar.x) < 1.2f &&
                   localDirToCar.z > 0 &&
                   localDirToCar.z < 8f;
        }

        private Collider GetBestTargetCar()
        {
            float bestScore = -1f;
            Collider bestTarget = null;

            foreach (var collider in _nearbyColliders)
            {
                Vector3 directionToCar = collider.transform.position - transform.position;
                Vector3 localDirToCar = transform.InverseTransformDirection(directionToCar);
                float distance = directionToCar.magnitude;

                // Игнорируем машины позади
                if (localDirToCar.z < 0) continue;

                // Рассчитываем оценку цели на основе расстояния и расположения
                float distanceScore = 1f - Mathf.Clamp01(distance / _collisionDetectionDistance);
                float positionScore = 0;

                if (IsValidTargetForSideHit(localDirToCar))
                {
                    positionScore = 0.8f;
                }
                else if (IsValidTargetForRearHit(localDirToCar))
                {
                    positionScore = 1f;
                }
                else
                {
                    positionScore = 0.3f;
                }

                float finalScore = distanceScore * positionScore;

                if (finalScore > bestScore)
                {
                    bestScore = finalScore;
                    bestTarget = collider;
                }
            }

            return bestTarget;
        }

        private void HandleCarCollision(Collision collision)
        {
            // Снижаем скорость после столкновения
            _currentThrottleInput *= 0.4f;

            // Повышаем агрессивность после столкновения
            _aggressionFactor = Mathf.Clamp01(_aggressionFactor + 0.15f);

            // Отталкиваемся от машины, с которой столкнулись
            if (_carRigidbody != null)
            {
                Rigidbody otherRb = collision.gameObject.GetComponentInParent<Rigidbody>();
                if (otherRb != null)
                {
                    Vector3 pushDirection = (transform.position - otherRb.transform.position).normalized;
                    pushDirection.y = 0;

                    _carRigidbody.AddForce(pushDirection * _sideHitForce * 0.7f, ForceMode.Impulse);

                    // Контр-рулежка при боковом столкновении
                    Vector3 localImpact = transform.InverseTransformDirection(collision.impulse);
                    if (Mathf.Abs(localImpact.x) > Mathf.Abs(localImpact.z))
                    {
                        _currentSteeringInput = -Mathf.Sign(localImpact.x) * 0.7f;
                    }
                }
            }
        }

        private void ApplyContinuousCollisionResponse(Collision collision)
        {
            // Продолжительный контакт - пытаемся выбраться, слегка толкая соперника
            Vector3 contactNormal = collision.contacts[0].normal;
            Vector3 escapeDirection = contactNormal;

            // Если застреваем сбоку от соперника
            Vector3 localContact = transform.InverseTransformDirection(contactNormal);
            if (Mathf.Abs(localContact.x) > Mathf.Abs(localContact.z))
            {
                // Боковой контакт
                _currentSteeringInput = Mathf.Sign(localContact.x) * 0.8f;
                _currentThrottleInput = 0.8f;
            }
            else
            {
                // Контакт спереди/сзади
                _currentThrottleInput = localContact.z > 0 ? -0.7f : 0.7f;
            }

            _carRigidbody.AddForce(escapeDirection * 2f, ForceMode.Impulse);
        }

        private void DetectOffTrackRecovery()
        {
            // Проверяем, далеко ли машина от гоночной линии
            float distanceToRacingLine = Vector3.Distance(transform.position, _targetPoint);
            _isOffRacingLine = distanceToRacingLine > _waypointReachedDistance * 2.5f;

            // Если машина далеко от трассы или застряла
            bool stucked = _carController.CurrentSpeed < 5f && Mathf.Abs(_currentThrottleInput) > 0.5f;

            if ((_isOffRacingLine || stucked) && !_isRecovering)
            {
                _recoveryTimer += Time.deltaTime;

                // Если долго не можем вернуться на трассу, запускаем восстановление
                if (_recoveryTimer > 3.0f)
                {
                    _isRecovering = true;
                    _recoveryTimer = 0;
                }
            }
            else
            {
                _recoveryTimer = 0;
            }
        }

        private void PerformRecovery()
        {
            // Восстановление после схода с трассы или застревания
            Vector3 directionToTarget = _targetPoint - transform.position;
            Vector3 localDirection = transform.InverseTransformDirection(directionToTarget);

            // Резкое руление в сторону гоночной линии
            float targetAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            _currentSteeringInput = Mathf.Clamp(targetAngle / 30f * _recoverySteeringStrength, -1f, 1f);

            // В зависимости от положения относительно трассы - газ или тормоз
            float distanceToRacingLine = directionToTarget.magnitude;
            if (distanceToRacingLine < _waypointReachedDistance)
            {
                // Достаточно близко к трассе, завершаем восстановление
                _isRecovering = false;
                _currentThrottleInput = 0.5f;
            }
            else
            {
                float dot = Vector3.Dot(transform.forward, directionToTarget.normalized);
                if (dot > 0)
                {
                    // Машина направлена к трассе - газуем
                    _currentThrottleInput = 0.8f;
                }
                else
                {
                    // Машина направлена от трассы - тормозим и разворачиваемся
                    _currentThrottleInput = -0.6f;
                }
            }

            _recoveryTimer += Time.deltaTime;
            if (_recoveryTimer > 5.0f)
            {
                // Если восстановление занимает слишком много времени, прекращаем его
                _isRecovering = false;
                _recoveryTimer = 0;
            }
        }

        #endregion
    }
}