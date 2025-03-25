using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace MiniRace.Environment
{
    [RequireComponent(typeof(SplineContainer))]
    public class Road : MonoBehaviour
    {
        #region --- Members ---

        [Header("Objects")]
        private SplineContainer _spline;

        [Header("Settings")]
        [SerializeField] private int _checkPointsAmount;

        [Header("Variables")]
        private List<RoadSegment> _segments = new List<RoadSegment>();

        #endregion

        #region --- Properties ---

        public List<RoadSegment> Segments { get => _segments; }

        #endregion

        #region --- Control Methods ---

        public void Inistialize()
        {
            _spline = GetComponent<SplineContainer>();
            CreateRoadSegments();
        }
        private void CreateRoadSegments()
        {
            for (int i = 0; i < _checkPointsAmount; i++)
            {
                float3 startPoint, startTangent, startUp;
                float3 endPoint, endTangent, endUp;
                _spline.Evaluate((float)i / (_checkPointsAmount + 1), out startPoint, out startTangent, out startUp);
                _spline.Evaluate((float)(i + 1) / (_checkPointsAmount + 1), out endPoint, out endTangent, out endUp);

                _segments.Add(new RoadSegment(startPoint, startTangent, endPoint, endTangent));
            }
        }

        #endregion
    }
}

