using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using MiniRace.Control;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace MiniRace.Game
{
    public class CameraController : MonoBehaviour
    {
        #region --- Enums ---

        public enum StateType
        {
            Menu,
            Game
        }

        #endregion

        #region --- Classes ---

        [Serializable]
        private class CameraStateInfo
        {
            [SerializeField] private StateType _type;
            [SerializeField] private Vector3 _position;
            [SerializeField] private Vector3 _eulerRotation;
            [SerializeField] private bool _isFolowing;

            public StateType Type { get => _type; }
            public Vector3 Position { get => _position; }
            public Vector3 EulerRotation { get => _eulerRotation; }
            public bool IsFolowing { get => _isFolowing; }
        }

        #endregion

        #region --- Members ---

        [Header("Objects")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private SplineContainer _movingToGamingModeSpline;

        [Header("Settings")]
        [SerializeField] private CameraStateInfo[] _cameraStateInfos;
        [SerializeField] private float _followSpeed;
        [SerializeField] private float _lookSpeed;
        [SerializeField] private float _moveToGamingModeDuration;

        [Header("Variables")]
        private Vector3 _offset;
        private CameraStateInfo _currentCameraStateInfo;

        #endregion

        #region --- Mono Override Methods ---

        private void Start()
        {
            Initialize();
            AdjustState(StateType.Menu);
            CalculateOffset();
        }
        private void FixedUpdate()
        {
            if (_currentCameraStateInfo.IsFolowing) Follow();
        }
        private void OnDestroy()
        {
            GameManager.Instance.OnRaceStarting -= CallMoveToGamingMode;
        }

        #endregion

        #region --- Control Methods ---

        private void Initialize()
        {
            GameManager.Instance.OnRaceStarting += CallMoveToGamingMode;
            AdjustState(StateType.Menu);
        }
        private void CallMoveToGamingMode(int delayBeforeStart)
        {
            MoveToGamingMode().Forget();
        }
        private async UniTask MoveToGamingMode()
        {
            CameraStateInfo stateInfo = _cameraStateInfos.First((info) => info.Type == StateType.Game);
            for (float t = 0; t < _moveToGamingModeDuration; t += Time.deltaTime)
            {
                float normalizedTime = t / _moveToGamingModeDuration;

                float3 position, tangent, up;
                _movingToGamingModeSpline.Evaluate(normalizedTime, out position, out tangent, out up);
                transform.position = position;

                float influenceFactor = Mathf.Clamp01(normalizedTime);
                Vector3 lookDirection = Vector3.Lerp(_followTarget.position - transform.position, stateInfo.Position - transform.position, influenceFactor);

                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                transform.rotation = targetRotation;

                await UniTask.Yield();
            }

            AdjustState(StateType.Game);
            CalculateOffset();
        }
        private void AdjustState(StateType type)
        {
            _currentCameraStateInfo = _cameraStateInfos.First((info) => info.Type == type);

            transform.rotation = Quaternion.Euler(_currentCameraStateInfo.EulerRotation);
            transform.position = _currentCameraStateInfo.Position;
        }
        private void CalculateOffset()
        {
            _offset = _currentCameraStateInfo.Position - _followTarget.position;
        }
        private void Follow()
        {
            Vector3 forwardDirection = _followTarget.forward;
            Vector3 targetPos = _followTarget.position - forwardDirection * _offset.magnitude + Vector3.up * _offset.y;

            transform.position = Vector3.Lerp(transform.position, targetPos, _followSpeed * Time.deltaTime);

            Vector3 lookDirection = _followTarget.position - transform.position;
            Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, _lookSpeed * Time.deltaTime);
        }

        #endregion
    }
}