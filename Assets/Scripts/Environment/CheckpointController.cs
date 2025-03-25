using System;
using System.Collections.Generic;
using MiniRace.Control;
using MiniRace.Game;
using UnityEngine;

namespace MiniRace.Environment
{
    public class CheckpointController : MonoBehaviour, ICarPositionTracker
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

        #region --- Properties ---

        public int CurrentLap { get; private set; }
        public int CurrentCheckpointIndex { get; private set; }
        public float DistanceToNextCheckpoint { get; private set; }
        public bool IsPlayer { get; private set; }

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
            GameManager.Instance.OnRaceStarted += () => _checkPoint.gameObject.SetActive(true);
            GameManager.Instance.OnRaceFinished += () => _checkPoint.gameObject.SetActive(false);
            _checkPoint.transform.position = new Vector3(_roadSegments[_currentSegmentIndex].EndPoint.x, _checkPoint.transform.position.y, _roadSegments[_currentSegmentIndex].EndPoint.z);

            RacePositionManager.Instance.RegisterCar(this);
            IsPlayer = true;
        }
        private void CheckForCheckpointClaim()
        {
            DistanceToNextCheckpoint = Vector3.Distance(_playerCar.position, _roadSegments[_currentSegmentIndex].EndPoint);
            if (DistanceToNextCheckpoint < _distanceToClaimCheckPoint)
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
                CurrentLap++;
                if (CurrentLap >= GameManager.Instance.LapAmount) GameManager.Instance.CallFinishRace();
            }
            CurrentCheckpointIndex = _currentSegmentIndex;
            _checkPoint.transform.position = new Vector3(_roadSegments[_currentSegmentIndex].EndPoint.x, _checkPoint.transform.position.y, _roadSegments[_currentSegmentIndex].EndPoint.z);
            _checkPoint.Play();
        }

        #endregion
    }
}