using System;
using UnityEngine;

namespace MiniRace
{
    [Serializable]
    public class WheelInfo
    {
        #region --- Members ---

        [Header("Objects")]
        [SerializeField] private GameObject _wheelMesh;
        [SerializeField] private WheelCollider _wheelCollider;
        [SerializeField] private ParticleSystem _driftEffect;
        [SerializeField] private TrailRenderer _skid;

        [Header("Settings")]
        [SerializeField] private bool _isSteeringWheel;
        [SerializeField] private bool _applyMotorTorque;

        [Header("Variables")]
        private WheelFrictionCurve _wheelFriction;
        private float _wheelExtremumSlip;

        #endregion

        #region  --- Properties ---

        public WheelCollider Collider { get => _wheelCollider; }
        public bool IsSteeringWheel { get => _isSteeringWheel; }
        public bool ApplyMotorTorque { get => _applyMotorTorque; }
        public ParticleSystem DriftEffect { get => _driftEffect; }
        public TrailRenderer Skid { get => _skid; }

        #endregion

        #region --- Control Methods ---

        public void Initialize()
        {
            if (_wheelCollider != null)
            {
                _wheelFriction = new WheelFrictionCurve();
                _wheelFriction.extremumSlip = _wheelCollider.sidewaysFriction.extremumSlip;
                _wheelExtremumSlip = _wheelCollider.sidewaysFriction.extremumSlip;
                _wheelFriction.extremumValue = _wheelCollider.sidewaysFriction.extremumValue;
                _wheelFriction.asymptoteSlip = _wheelCollider.sidewaysFriction.asymptoteSlip;
                _wheelFriction.asymptoteValue = _wheelCollider.sidewaysFriction.asymptoteValue;
                _wheelFriction.stiffness = _wheelCollider.sidewaysFriction.stiffness;
            }
        }
        public void ReduceTraction(float driftingAxis, int handbrakeDriftMultiplier)
        {
            if (_wheelCollider != null)
            {
                _wheelFriction.extremumSlip = _wheelExtremumSlip * handbrakeDriftMultiplier * driftingAxis;
                _wheelCollider.sidewaysFriction = _wheelFriction;
            }
        }
        public void RestoreTraction()
        {
            if (_wheelCollider != null)
            {
                _wheelFriction.extremumSlip = _wheelExtremumSlip;
                _wheelCollider.sidewaysFriction = _wheelFriction;
            }
        }
        public void AnimateWheelMesh()
        {
            if (_wheelMesh != null && _wheelCollider != null)
            {
                Vector3 position;
                Quaternion rotation;

                _wheelCollider.GetWorldPose(out position, out rotation);

                _wheelMesh.transform.position = position;
                _wheelMesh.transform.rotation = Quaternion.Lerp(_wheelMesh.transform.rotation, rotation, Time.deltaTime * 10f);
            }
        }

        #endregion
    }
}