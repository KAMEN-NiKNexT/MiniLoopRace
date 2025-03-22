using System.Collections.Generic;
using MiniRace;
using UnityEngine;

namespace Car
{
    public class WheelsHandler : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private List<WheelInfo> _wheels = new List<WheelInfo>();

        #endregion

        #region Private Fields

        // Список колес для управления поворотом
        private List<WheelInfo> _steeringWheels = new List<WheelInfo>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Текущее значение оси поворота (-1 до 1)
        /// </summary>
        public float SteeringAxis { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализация данных о трении колес
        /// </summary>
        public void Initialize()
        {
            // Инициализируем каждое колесо
            foreach (var wheel in _wheels)
            {
                wheel.Initialize();

                // Добавляем колесо в список поворачиваемых, если оно помечено как управляемое
                if (wheel.IsSteeringWheel)
                {
                    _steeringWheels.Add(wheel);
                }
            }
        }

        /// <summary>
        /// Рассчитать текущую скорость автомобиля в км/ч
        /// </summary>
        public float CalculateCarSpeed()
        {
            // Используем первое колесо для расчета скорости
            if (_wheels.Count > 0)
            {
                return 2 * Mathf.PI * _wheels[0].Collider.radius * _wheels[0].Collider.rpm * 60 / 1000;
            }
            return 0f;
        }

        /// <summary>
        /// Поворот колес влево
        /// </summary>
        public void TurnLeft(float steeringSpeed, int maxSteeringAngle)
        {
            SteeringAxis = SteeringAxis - (Time.deltaTime * 10f * steeringSpeed);
            if (SteeringAxis < -1f)
            {
                SteeringAxis = -1f;
            }

            var steeringAngle = SteeringAxis * maxSteeringAngle;
            foreach (var wheel in _steeringWheels)
            {
                wheel.Collider.steerAngle = Mathf.Lerp(wheel.Collider.steerAngle, steeringAngle, steeringSpeed);
            }
        }

        /// <summary>
        /// Поворот колес вправо
        /// </summary>
        public void TurnRight(float steeringSpeed, int maxSteeringAngle)
        {
            SteeringAxis = SteeringAxis + (Time.deltaTime * 10f * steeringSpeed);
            if (SteeringAxis > 1f)
            {
                SteeringAxis = 1f;
            }

            var steeringAngle = SteeringAxis * maxSteeringAngle;
            foreach (var wheel in _steeringWheels)
            {
                wheel.Collider.steerAngle = Mathf.Lerp(wheel.Collider.steerAngle, steeringAngle, steeringSpeed);
            }
        }

        /// <summary>
        /// Сброс поворота колес
        /// </summary>
        public void ResetSteeringAngle(float steeringSpeed)
        {
            if (SteeringAxis < 0f)
            {
                SteeringAxis = SteeringAxis + (Time.deltaTime * 10f * steeringSpeed);
            }
            else if (SteeringAxis > 0f)
            {
                SteeringAxis = SteeringAxis - (Time.deltaTime * 10f * steeringSpeed);
            }

            if (_steeringWheels.Count > 0 && Mathf.Abs(_steeringWheels[0].Collider.steerAngle) < 1f)
            {
                SteeringAxis = 0f;
            }

            var steeringAngle = SteeringAxis * 0; // устанавливаем угол в 0
            foreach (var wheel in _steeringWheels)
            {
                wheel.Collider.steerAngle = Mathf.Lerp(wheel.Collider.steerAngle, steeringAngle, steeringSpeed);
            }
        }

        /// <summary>
        /// Применение ускорения к колесам
        /// </summary>
        public void ApplyMotorTorque(float motorTorque)
        {
            foreach (var wheel in _wheels)
            {
                if (wheel.ApplyMotorTorque)
                {
                    wheel.Collider.motorTorque = motorTorque;
                }
            }
        }

        /// <summary>
        /// Применение торможения к колесам
        /// </summary>
        public void ApplyBrakeTorque(float brakeTorque)
        {
            foreach (var wheel in _wheels)
            {
                wheel.Collider.brakeTorque = brakeTorque;
            }
        }

        /// <summary>
        /// Уменьшение сцепления колес с дорогой (для заноса)
        /// </summary>
        public void ReduceTraction(float driftingAxis, int handbrakeDriftMultiplier)
        {
            foreach (var wheel in _wheels)
            {
                wheel.ReduceTraction(driftingAxis, handbrakeDriftMultiplier);
            }
        }

        /// <summary>
        /// Восстановление сцепления колес с дорогой
        /// </summary>
        public void RestoreTraction()
        {
            foreach (var wheel in _wheels)
            {
                wheel.RestoreTraction();
            }
        }

        /// <summary>
        /// Анимация вращения 3D моделей колес
        /// </summary>
        public void AnimateWheelMeshes()
        {
            foreach (var wheel in _wheels)
            {
                wheel.AnimateWheelMesh();
            }
        }

        #endregion
    }
}