using System.Collections;
using Car;
using UnityEngine;

namespace MiniRace
{
    public class ModernCarController : BaseCarController
    {
        #region Serialized Fields

        [Header("Car Settings")]
        [SerializeField] private CarSettings _carSettings;

        [Header("Wheels")]
        [SerializeField] private WheelsHandler _wheelsHandler;

        //[Header("Effects")]
        //[SerializeField] private CarEffectsManager _effectsManager;

        #endregion

        #region Private Fields

        private Rigidbody _carRigidbody;
        private ICarInput _carInput;

        // Значения для дрифта
        private float _driftingAxis;

        // Значения для движения
        private float _throttleAxis;

        // Значения для физики
        private float _localVelocityX;
        private float _localVelocityZ;

        // Флаги состояния автомобиля
        private bool _isDecelerating;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _carRigidbody = GetComponent<Rigidbody>();
            _carInput = GetComponent<ICarInput>();

            if (_carInput == null)
            {
                // Если не найден компонент ввода, используем клавиатурный ввод по умолчанию
                _carInput = gameObject.AddComponent<KeyboardCarInput>();
            }
        }

        private void Start()
        {
            InitializeCar();

            // Запускаем периодическое обновление эффектов и UI
            InvokeRepeating(nameof(UpdateEffects), 0f, 0.1f);
        }

        private void Update()
        {
            UpdateCarData();
            HandleInputs();
            _wheelsHandler.AnimateWheelMeshes();
        }

        #endregion

        #region Overridden Methods

        /// <summary>
        /// Инициализация автомобиля
        /// </summary>
        protected override void InitializeCar()
        {
            // Настройка центра масс
            if (_carRigidbody != null)
            {
                _carRigidbody.centerOfMass = _carSettings.BodyMassCenter;
            }

            // Инициализация колес
            _wheelsHandler.Initialize();
        }

        /// <summary>
        /// Ускорение автомобиля
        /// </summary>
        public override void Accelerate(float throttleInput)
        {
            // Проверка заноса на основе боковой скорости
            if (Mathf.Abs(_localVelocityX) > 2.5f)
            {
                IsDrifting = true;
            }
            else
            {
                IsDrifting = false;
            }

            // Плавное увеличение оси газа
            _throttleAxis = _throttleAxis + (Time.deltaTime * 3f);
            if (_throttleAxis > 1f)
            {
                _throttleAxis = 1f;
            }

            // Проверка движения назад
            if (_localVelocityZ < -1f)
            {
                ApplyBrakes();
            }
            else
            {
                if (Mathf.RoundToInt(CurrentSpeed) < _carSettings.MaxSpeed)
                {
                    // Применение крутящего момента к колесам
                    float motorTorque = (_carSettings.AccelerationMultiplier * 50f) * _throttleAxis;
                    _wheelsHandler.ApplyBrakeTorque(0);
                    _wheelsHandler.ApplyMotorTorque(motorTorque);
                }
                else
                {
                    // Если достигнута максимальная скорость, прекращаем ускорение
                    _wheelsHandler.ApplyMotorTorque(0);
                }
            }
        }

        /// <summary>
        /// Движение назад
        /// </summary>
        public override void Reverse(float throttleInput)
        {
            // Проверка заноса на основе боковой скорости
            if (Mathf.Abs(_localVelocityX) > 2.5f)
            {
                IsDrifting = true;
            }
            else
            {
                IsDrifting = false;
            }

            // Плавное уменьшение оси газа (для заднего хода)
            _throttleAxis = _throttleAxis - (Time.deltaTime * 3f);
            if (_throttleAxis < -1f)
            {
                _throttleAxis = -1f;
            }

            // Проверка движения вперед
            if (_localVelocityZ > 1f)
            {
                ApplyBrakes();
            }
            else
            {
                if (Mathf.Abs(Mathf.RoundToInt(CurrentSpeed)) < _carSettings.MaxReverseSpeed)
                {
                    // Применение отрицательного крутящего момента к колесам
                    float motorTorque = (_carSettings.AccelerationMultiplier * 50f) * _throttleAxis;
                    _wheelsHandler.ApplyBrakeTorque(0);
                    _wheelsHandler.ApplyMotorTorque(motorTorque);
                }
                else
                {
                    // Если достигнута максимальная скорость заднего хода, прекращаем ускорение
                    _wheelsHandler.ApplyMotorTorque(0);
                }
            }
        }

        /// <summary>
        /// Поворот автомобиля
        /// </summary>
        public override void Steer(float steeringInput)
        {
            if (steeringInput < 0)
            {
                _wheelsHandler.TurnLeft(_carSettings.SteeringSpeed, _carSettings.MaxSteeringAngle);
            }
            else if (steeringInput > 0)
            {
                _wheelsHandler.TurnRight(_carSettings.SteeringSpeed, _carSettings.MaxSteeringAngle);
            }
            else if (steeringInput == 0 && _wheelsHandler.SteeringAxis != 0f)
            {
                _wheelsHandler.ResetSteeringAngle(_carSettings.SteeringSpeed);
            }
        }

        /// <summary>
        /// Применение ручного тормоза
        /// </summary>
        public override void ApplyHandbrake()
        {
            CancelInvoke(nameof(RecoverTraction));

            // Постепенное увеличение значения дрифта
            _driftingAxis = _driftingAxis + (Time.deltaTime);
            if (_driftingAxis > 1f)
            {
                _driftingAxis = 1f;
            }

            // Проверка заноса на основе боковой скорости
            if (Mathf.Abs(_localVelocityX) > 2.5f)
            {
                IsDrifting = true;
            }
            else
            {
                IsDrifting = false;
            }

            // Уменьшение сцепления с дорогой
            _wheelsHandler.ReduceTraction(_driftingAxis, _carSettings.HandbrakeDriftMultiplier);

            // Устанавливаем флаг блокировки сцепления
            IsTractionLocked = true;
        }

        /// <summary>
        /// Отпускание ручного тормоза
        /// </summary>
        public override void ReleaseHandbrake()
        {
            IsTractionLocked = false;

            // Постепенное восстановление сцепления
            _driftingAxis = _driftingAxis - (Time.deltaTime / 1.5f);
            if (_driftingAxis < 0f)
            {
                _driftingAxis = 0f;
            }

            if (_driftingAxis > 0)
            {
                // Продолжаем постепенно восстанавливать сцепление
                _wheelsHandler.ReduceTraction(_driftingAxis, _carSettings.HandbrakeDriftMultiplier);
                Invoke(nameof(RecoverTraction), Time.deltaTime);
            }
            else
            {
                // Полностью восстанавливаем сцепление
                _wheelsHandler.RestoreTraction();
                _driftingAxis = 0f;
            }
        }

        /// <summary>
        /// Прекращение ускорения (отпускание педали газа)
        /// </summary>
        public override void ReleaseThrottle()
        {
            _wheelsHandler.ApplyMotorTorque(0);
        }

        /// <summary>
        /// Обновление данных автомобиля
        /// </summary>
        protected override void UpdateCarData()
        {
            // Обновляем скорость автомобиля
            CurrentSpeed = _wheelsHandler.CalculateCarSpeed();

            // Получаем локальные скорости для определения направления движения и заноса
            _localVelocityX = transform.InverseTransformDirection(_carRigidbody.linearVelocity).x;
            _localVelocityZ = transform.InverseTransformDirection(_carRigidbody.linearVelocity).z;
        }

        /// <summary>
        /// Применение тормозов
        /// </summary>
        protected override void ApplyBrakes()
        {
            _wheelsHandler.ApplyBrakeTorque(_carSettings.BrakeForce);
        }

        /// <summary>
        /// Замедление автомобиля при отпускании газа и тормоза
        /// </summary>
        protected override void DecelerateCar()
        {
            // Проверка заноса на основе боковой скорости
            if (Mathf.Abs(_localVelocityX) > 2.5f)
            {
                IsDrifting = true;
            }
            else
            {
                IsDrifting = false;
            }

            // Плавное сбрасывание значения оси газа до нуля
            if (_throttleAxis != 0f)
            {
                if (_throttleAxis > 0f)
                {
                    _throttleAxis = _throttleAxis - (Time.deltaTime * 10f);
                }
                else if (_throttleAxis < 0f)
                {
                    _throttleAxis = _throttleAxis + (Time.deltaTime * 10f);
                }

                if (Mathf.Abs(_throttleAxis) < 0.15f)
                {
                    _throttleAxis = 0f;
                }
            }

            // Применяем замедление к физическому телу
            _carRigidbody.linearVelocity = _carRigidbody.linearVelocity * (1f / (1f + (0.025f * _carSettings.DecelerationMultiplier)));

            // Убираем крутящий момент с колес
            _wheelsHandler.ApplyMotorTorque(0);

            // Если скорость очень низкая, полностью останавливаем автомобиль
            if (_carRigidbody.linearVelocity.magnitude < 0.25f)
            {
                _carRigidbody.linearVelocity = Vector3.zero;
                CancelInvoke(nameof(DecelerateCar));
                _isDecelerating = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обработка пользовательского ввода
        /// </summary>
        private void HandleInputs()
        {
            // Обновляем данные ввода
            _carInput.UpdateInput();

            // Обработка газа/тормоза
            if (_carInput.ThrottleInput > 0)
            {
                CancelInvoke(nameof(DecelerateCar));
                _isDecelerating = false;
                Accelerate(_carInput.ThrottleInput);
            }
            else if (_carInput.ThrottleInput < 0)
            {
                CancelInvoke(nameof(DecelerateCar));
                _isDecelerating = false;
                Reverse(_carInput.ThrottleInput);
            }
            else
            {
                ReleaseThrottle();

                if (!_isDecelerating && !_carInput.IsHandbrakeActive)
                {
                    InvokeRepeating(nameof(DecelerateCar), 0f, 0.1f);
                    _isDecelerating = true;
                }
            }

            Steer(_carInput.SteeringInput);
            if (_carInput.IsHandbrakeActive)
            {
                CancelInvoke(nameof(DecelerateCar));
                _isDecelerating = false;
                ApplyHandbrake();
            }
            else if (!_carInput.IsHandbrakeActive && IsTractionLocked)
            {
                ReleaseHandbrake();
            }
        }

        private void UpdateEffects()
        {
            // Placeholder for effects update
            // To be implemented when effects manager is added
        }

        private void RecoverTraction()
        {
            ReleaseHandbrake();
        }

        #endregion
    }
}