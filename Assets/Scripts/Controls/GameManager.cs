using System;
using Cysharp.Threading.Tasks;
using MiniRace.Environment;
using UnityEngine;

namespace MiniRace.Control
{
    public class GameManager : SingletonComponent<GameManager>
    {
        #region --- Members ---

        [Header("Objects")]
        [SerializeField] private Road _road;
        [SerializeField] private CheckpointController _checkpointController;
        [SerializeField] private AICarInput[] _enemyAI;

        [Header("Settings")]
        [SerializeField] private int _targetFrameRate;
        [SerializeField] private int _delayBeforeStartRace;

        #endregion

        #region --- Events ---

        public event Action<int> OnGameStarting;
        public event Action OnGameStarted;

        #endregion

        #region --- Mono Override Methods ---

        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = _targetFrameRate;
            Setup();
        }

        #endregion

        #region --- Control Methods ---

        private void Setup()
        {
            _road.Inistialize();
            _checkpointController.Initialize(_road.Segments);
            for (int i = 0; i < _enemyAI.Length; i++)
            {
                _enemyAI[i].Initialize(_road.Segments);
            }
        }
        public void CallStartRace()
        {
            StartRace().Forget();
        }
        private async UniTask StartRace()
        {
            OnGameStarting?.Invoke(_delayBeforeStartRace);
            await UniTask.WaitForSeconds(_delayBeforeStartRace);

            OnGameStarted?.Invoke();
        }

        #endregion
    }
}