using System;
using System.Collections.Generic;
using MiniRace.Control;
using UnityEngine;

namespace MiniRace.Environment
{
    public class CheckpointController : MonoBehaviour
    {
        #region --- Members ---

        [Header("Objects")]
        [SerializeField] private ParticleSystem _checkPoint;
        [SerializeField] private Transform _playerCar;
        private List<RoadSegment> _roadSegments;

        [Header("Settings")]
        [SerializeField] private float _distanceToClaimCheckPoint;

        [Header("Variables")]
        private int _currentSegmentIndex;
        public event Action OnLapCompleted;

        #endregion

        #region --- Mono Override Methods ---

        private void Update()
        {
            if (_checkPoint.gameObject.activeSelf) CheckForCheckpointClaim();
        }

        #endregion

        #region --- Control Methods ---

        public void Initialize(List<RoadSegment> roadSegments)
        {
            _roadSegments = roadSegments;
            GameManager.Instance.OnGameStarted += EnableCheckPoint;
            _checkPoint.transform.position = new Vector3(_roadSegments[_currentSegmentIndex].StartPoint.x, _checkPoint.transform.position.y, _roadSegments[_currentSegmentIndex].StartPoint.z);
        }
        private void CheckForCheckpointClaim()
        {
            float distanceToCheckpoint = Vector3.Distance(_playerCar.position, _roadSegments[_currentSegmentIndex].StartPoint);
            if (distanceToCheckpoint < _distanceToClaimCheckPoint)
            {
                ClaimCheckpoint();
            }
        }
        private void ClaimCheckpoint()
        {
            _currentSegmentIndex++;
            if (_currentSegmentIndex >= _roadSegments.Count)
            {
                _currentSegmentIndex = 0;
                OnLapCompleted?.Invoke();
            }
            _checkPoint.transform.position = new Vector3(_roadSegments[_currentSegmentIndex].StartPoint.x, _checkPoint.transform.position.y, _roadSegments[_currentSegmentIndex].StartPoint.z);
            _checkPoint.Play();
        }
        private void EnableCheckPoint()
        {
            _checkPoint.gameObject.SetActive(true);
        }

        #endregion
    }
}