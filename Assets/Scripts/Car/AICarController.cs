using UnityEngine;

namespace Car
{
    /// <summary>
    /// Контроллер автомобиля с ИИ
    /// </summary>
    public class AICarController : MonoBehaviour, ICarInput
    {
        #region Serialized Fields

        [Header("AI Settings")]
        [Tooltip("Точки пути для ИИ")]
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("Расстояние достижения точки пути")]
        [SerializeField] private float _waypointThreshold = 5f;

        [Tooltip("Максимальный угол поворота для полного руления")]
        [SerializeField] private float _maxSteeringAngle = 30f;

        [Tooltip("Активировать ли занос на поворотах")]
        [SerializeField] private bool _useDrifting = true;

        [Tooltip("Минимальный угол поворота для заноса")]
        [SerializeField] private float _driftAngleThreshold = 45f;

        #endregion

        #region Private Fields

        private int _currentWaypointIndex = 0;
        private float _currentThrottleInput = 0f;
        private float _currentSteeringInput = 0f;
        private bool _currentHandbrakeInput = false;

        #endregion

        #region Properties

        /// <summary>
        /// Значение ускорения (от -1 до 1)
        /// </summary>
        public float ThrottleInput => _currentThrottleInput;

        /// <summary>
        /// Значение поворота (от -1 до 1)
        /// </summary>
        public float SteeringInput => _currentSteeringInput;

        /// <summary>
        /// Нажат ли ручной тормоз
        /// </summary>
        public bool IsHandbrakeActive => _currentHandbrakeInput;

        #endregion

        #region Unity Methods

        private void Start()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogError("AI Car Controller needs waypoints to operate!");
                enabled = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обновление входных данных для автомобиля на основе ИИ
        /// </summary>
        public void UpdateInput()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            Transform currentWaypoint = _waypoints[_currentWaypointIndex];

            // Проверяем, достигли ли текущей точки
            if (Vector3.Distance(transform.position, currentWaypoint.position) < _waypointThreshold)
            {
                // Переходим к следующей точке
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                currentWaypoint = _waypoints[_currentWaypointIndex];
            }

            // Рассчитываем вектор направления к следующей точке пути
            Vector3 directionToWaypoint = currentWaypoint.position - transform.position;

            // Вычисляем угол между текущим направлением автомобиля и направлением к точке
            Vector3 localDirection = transform.InverseTransformDirection(directionToWaypoint);
            float angleToWaypoint = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

            // Нормализуем угол для получения значения от -1 до 1 для поворота
            _currentSteeringInput = Mathf.Clamp(angleToWaypoint / _maxSteeringAngle, -1f, 1f);

            // Рассчитываем значение газа/тормоза
            // Чем больше угол поворота, тем меньше газа (чтобы не вылетать на поворотах)
            float throttleMultiplier = 1f - Mathf.Abs(_currentSteeringInput) * 0.5f;
            _currentThrottleInput = throttleMultiplier;

            // Определяем, нужно ли использовать ручной тормоз для заноса
            if (_useDrifting && Mathf.Abs(angleToWaypoint) > _driftAngleThreshold)
            {
                _currentHandbrakeInput = true;
            }
            else
            {
                _currentHandbrakeInput = false;
            }
        }

        #endregion
    }
}