using System;
using Unity.Mathematics;
using UnityEngine;

namespace MiniRace.Environment
{
    [Serializable]
    public class RoadSegment
    {
        #region  --- Members ---

        [SerializeField] private Vector3 _startPoint;
        [SerializeField] private Vector3 _startTangent;
        [SerializeField] private Vector3 _endPoint;
        [SerializeField] private Vector3 _endTangent;
        [SerializeField] private float _turnAngle;

        #endregion

        #region --- Properties ---

        public Vector3 StartPoint { get => _startPoint; }
        public Vector3 StartTangent { get => _startTangent; }
        public Vector3 EndPoint { get => _endPoint; }
        public Vector3 EndTangent { get => _endTangent; }
        public float TurnAngle { get => _turnAngle; }

        #endregion

        #region --- Constructors ---

        public RoadSegment(float3 startPoint, float3 startTangent, float3 endPoint, float3 endTangent)
        {
            _startPoint = startPoint;
            _startTangent = startTangent;

            _endPoint = endPoint;
            _endTangent = endTangent;

            _turnAngle = Vector3.Angle(_startTangent, _endTangent);
        }

        #endregion
    }
}