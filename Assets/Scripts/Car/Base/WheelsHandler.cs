using System.Collections.Generic;
using MiniRace;
using UnityEngine;

namespace MiniRace
{
    public class WheelsHandler : MonoBehaviour
    {
        #region --- Members ---

        [Header("Settings")]
        [SerializeField] private WheelInfo[] _wheels;


        [Header("Variables")]
        private List<WheelInfo> _steeringWheels = new List<WheelInfo>();

        #endregion

        #region --- Properties ---

        public float SteeringAxis { get; private set; }

        #endregion

        #region --- Control Methods ---

        public void Initialize()
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i].Initialize();

                if (_wheels[i].IsSteeringWheel)
                {
                    _steeringWheels.Add(_wheels[i]);
                }

                // Инициализация эффектов для каждого колеса
                if (_wheels[i].DriftEffect != null) _wheels[i].DriftEffect.Stop();
                if (_wheels[i].Skid != null)
                {
                    _wheels[i].Skid.emitting = false;
                }
            }
        }

        public void UpdateDriftEffects(bool isDrifing)
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                // Управление частицами дрифта
                if (_wheels[i].DriftEffect != null)
                {
                    if (isDrifing)
                    {
                        _wheels[i].DriftEffect.Play();
                    }
                    else
                    {
                        _wheels[i].DriftEffect.Stop();
                    }
                }

                // Управление следами от шин
                if (_wheels[i].Skid != null)
                {
                    _wheels[i].Skid.emitting = isDrifing;
                }
            }
        }

        public void TurnLeft(float steeringSpeed, int maxSteeringAngle)
        {
            Turn(-1, steeringSpeed, maxSteeringAngle);
        }
        public void TurnRight(float steeringSpeed, int maxSteeringAngle)
        {
            Turn(1, steeringSpeed, maxSteeringAngle);
        }
        public float CalculateCarSpeed()
        {
            if (_wheels.Length > 0)
            {
                return 2 * Mathf.PI * _wheels[0].Collider.radius * _wheels[0].Collider.rpm * 60 / 1000;
            }
            return 0f;
        }
        private void Update()
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                //Debug.LogError(_wheels[i].Collider.motorTorque + " motorTorque " + i);
                //Debug.LogError(_wheels[i].Collider.brakeTorque + " brakeTorque " + i);
                //Debug.LogError(_wheels[i].Collider.rpm + " rpm " + i);
            }
        }
        private void Turn(float direction, float steeringSpeed, int maxSteeringAngle)
        {
            SteeringAxis = Time.deltaTime * 10f * steeringSpeed * direction;
            SteeringAxis = Mathf.Clamp(SteeringAxis, -1f, 1f);

            var steeringAngle = SteeringAxis * maxSteeringAngle;
            for (int i = 0; i < _steeringWheels.Count; i++)
            {
                _steeringWheels[i].Collider.steerAngle = Mathf.Lerp(_steeringWheels[i].Collider.steerAngle, steeringAngle, steeringSpeed);
            }
        }
        public void ResetSteeringAngle(float steeringSpeed)
        {
            if (SteeringAxis < 0f)
            {
                SteeringAxis += Time.deltaTime * 10f * steeringSpeed;
            }
            else if (SteeringAxis > 0f)
            {
                SteeringAxis -= Time.deltaTime * 10f * steeringSpeed;
            }

            if (_steeringWheels.Count > 0 && Mathf.Abs(_steeringWheels[0].Collider.steerAngle) < 1f)
            {
                SteeringAxis = 0f;
            }

            var steeringAngle = SteeringAxis * 0;
            for (int i = 0; i < _steeringWheels.Count; i++)
            {
                _steeringWheels[i].Collider.steerAngle = Mathf.Lerp(_steeringWheels[i].Collider.steerAngle, steeringAngle, steeringSpeed);
            }
        }
        public void ApplyMotorTorque(float motorTorque)
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                if (_wheels[i].ApplyMotorTorque)
                {
                    _wheels[i].Collider.motorTorque = motorTorque;
                }
            }
        }
        public void ApplyBrakeTorque(float brakeTorque)
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i].Collider.brakeTorque = brakeTorque;
            }
        }
        public void ReduceTraction(float driftingAxis, int handbrakeDriftMultiplier)
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i].ReduceTraction(driftingAxis, handbrakeDriftMultiplier);
            }
        }
        public void RestoreTraction()
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i].RestoreTraction();
            }
        }
        public void AnimateWheelMeshes()
        {
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i].AnimateWheelMesh();
            }
        }

        #endregion
    }
}