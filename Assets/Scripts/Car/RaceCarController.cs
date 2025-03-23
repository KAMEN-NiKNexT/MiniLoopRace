using System;
using System.Collections;
using System.Threading;
using Car;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace MiniRace
{
    public class RaceCarController : BaseCarController
    {
        #region --- Members ---

        [Header("Components")]
        [SerializeField] private CarSettings _carSettings;
        [SerializeField] private WheelsHandler _wheelsHandler;

        [Header("Settings")]
        [SerializeField] private float _valueForDrift;

        [Header("Variables")]
        private Rigidbody _carRigidbody;
        private ICarInput _carInput;
        private float _driftingAxis;
        private float _throttleAxis;
        private float _localVelocityX;
        private float _localVelocityZ;
        private bool _isDecelerating;
        private CancellationTokenSource _cancelationTokenSource;

        #endregion

        #region --- Mono Override Methods ---

        private void Awake()
        {
            Initialize();
        }

        #endregion

        #region --- Control Methods ---

        private void HandleInputs()
        {
            if (_carInput.ThrottleInput > 0) Accelerate(_carInput.ThrottleInput);
            else if (_carInput.ThrottleInput < 0) Reverse(_carInput.ThrottleInput);
            else
            {
                ReleaseThrottle();

                if (!_isDecelerating && !_carInput.IsHandbrakeActive)
                {
                    StartDecelerationAsync().Forget();
                }
            }

            Steer(_carInput.SteeringInput);

            if (_carInput.IsHandbrakeActive) ApplyHandbrake();
            else if (!_carInput.IsHandbrakeActive && IsTractionLocked) ReleaseHandbrake();
        }

        #endregion

        #region --- BaseCarController Override Methods ---

        protected override void Initialize()
        {
            _carRigidbody = GetComponent<Rigidbody>();
            _carInput = GetComponent<ICarInput>();

            _carRigidbody.centerOfMass = _carSettings.BodyMassCenter;
            _wheelsHandler.Initialize();

            _carInput.OnInputUpdated += UpdateCar;
        }
        protected override void Accelerate(float throttleInput)
        {
            if (Mathf.Abs(_localVelocityX) > _valueForDrift) IsDrifting = true;
            else IsDrifting = false;

            _throttleAxis += Time.deltaTime * 3f;
            _throttleAxis = Mathf.Min(_throttleAxis, 1f);

            if (_localVelocityZ < -1f) ApplyBrakes();
            else
            {
                if (Mathf.RoundToInt(CurrentSpeed) < _carSettings.MaxSpeed)
                {
                    float motorTorque = _carSettings.AccelerationMultiplier * 50f * _throttleAxis;
                    _wheelsHandler.ApplyBrakeTorque(0);
                    _wheelsHandler.ApplyMotorTorque(motorTorque);
                }
                else
                {
                    _wheelsHandler.ApplyMotorTorque(0);
                }
            }
        }
        protected override void Reverse(float throttleInput)
        {
            if (Mathf.Abs(_localVelocityX) > _valueForDrift) IsDrifting = true;
            else IsDrifting = false;

            _throttleAxis -= Time.deltaTime * 3f;
            _throttleAxis = Mathf.Max(_throttleAxis, -1f);

            if (_localVelocityZ > 1f) ApplyBrakes();
            else
            {
                if (Mathf.Abs(Mathf.RoundToInt(CurrentSpeed)) < _carSettings.MaxReverseSpeed)
                {
                    float motorTorque = _carSettings.AccelerationMultiplier * 50f * _throttleAxis;
                    _wheelsHandler.ApplyBrakeTorque(0);
                    _wheelsHandler.ApplyMotorTorque(motorTorque);
                }
                else
                {
                    _wheelsHandler.ApplyMotorTorque(0);
                }
            }
        }

        protected override void Steer(float steeringInput)
        {
            if (steeringInput < 0) _wheelsHandler.TurnLeft(_carSettings.SteeringSpeed, _carSettings.MaxSteeringAngle);
            else if (steeringInput > 0) _wheelsHandler.TurnRight(_carSettings.SteeringSpeed, _carSettings.MaxSteeringAngle);
            else if (steeringInput == 0 && _wheelsHandler.SteeringAxis != 0f) _wheelsHandler.ResetSteeringAngle(_carSettings.SteeringSpeed);
        }
        protected override void ApplyHandbrake()
        {
            CancelInvoke(nameof(RecoverTraction));

            _driftingAxis = _driftingAxis + Time.deltaTime;
            _driftingAxis = Mathf.Min(_driftingAxis, 1f);

            if (Mathf.Abs(_localVelocityX) > _valueForDrift) IsDrifting = true;
            else IsDrifting = false;

            _wheelsHandler.ReduceTraction(_driftingAxis, _carSettings.HandbrakeDriftMultiplier);
            IsTractionLocked = true;
        }

        protected override void ReleaseHandbrake()
        {
            IsTractionLocked = false;

            _driftingAxis -= Time.deltaTime / 1.5f;
            _driftingAxis = Mathf.Max(_driftingAxis, 0f);

            if (_driftingAxis > 0)
            {
                _wheelsHandler.ReduceTraction(_driftingAxis, _carSettings.HandbrakeDriftMultiplier);
                Invoke(nameof(RecoverTraction), Time.deltaTime);
            }
            else
            {
                _wheelsHandler.RestoreTraction();
                _driftingAxis = 0f;
            }
        }

        protected override void ReleaseThrottle()
        {
            _wheelsHandler.ApplyMotorTorque(0);
        }
        protected void UpdateCar()
        {
            UpdateCarData();
            HandleInputs();
            _wheelsHandler.AnimateWheelMeshes();
        }
        protected override void UpdateCarData()
        {
            CurrentSpeed = _wheelsHandler.CalculateCarSpeed();

            _localVelocityX = transform.InverseTransformDirection(_carRigidbody.linearVelocity).x;
            _localVelocityZ = transform.InverseTransformDirection(_carRigidbody.linearVelocity).z;
        }

        protected override void ApplyBrakes()
        {
            _wheelsHandler.ApplyBrakeTorque(_carSettings.BrakeForce);
        }

        protected override void DecelerateCar()
        {
            if (Mathf.Abs(_localVelocityX) > _valueForDrift) IsDrifting = true;
            else IsDrifting = false;

            if (_throttleAxis != 0f)
            {
                if (_throttleAxis > 0f)
                {
                    _throttleAxis -= Time.deltaTime * 10f;
                }
                else if (_throttleAxis < 0f)
                {
                    _throttleAxis += Time.deltaTime * 10f;
                }

                if (Mathf.Abs(_throttleAxis) < 0.15f)
                {
                    _throttleAxis = 0f;
                }
            }

            _carRigidbody.linearVelocity *= 1f / (1f + (0.025f * _carSettings.DecelerationMultiplier));
            _wheelsHandler.ApplyMotorTorque(0);

            if (_carRigidbody.linearVelocity.magnitude < 0.25f)
            {
                _carRigidbody.linearVelocity = Vector3.zero;
                _isDecelerating = false;
            }
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid StartDecelerationAsync()
        {
            if (_isDecelerating) return;

            _isDecelerating = true;
            _cancelationTokenSource = new CancellationTokenSource();

            while (_isDecelerating)
            {
                DecelerateCar();
                await UniTask.WaitForSeconds(0.1f, cancellationToken: _cancelationTokenSource.Token);
            }
        }
        private void StopDeceleration()
        {
            if (_isDecelerating)
            {
                _cancelationTokenSource?.Cancel();
                _cancelationTokenSource?.Dispose();
                _cancelationTokenSource = null;
                _isDecelerating = false;
            }
        }

        private void UpdateEffects()
        {
        }

        private void RecoverTraction()
        {
            ReleaseHandbrake();
        }

        #endregion
    }
}