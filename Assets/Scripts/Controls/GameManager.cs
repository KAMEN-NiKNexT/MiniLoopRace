using System;
using Cysharp.Threading.Tasks;
using MiniRace.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private int _lapAmount;

        #endregion

        #region --- Events ---

        public event Action<int> OnRaceStarting;
        public event Action OnRaceStarted;
        public event Action OnRaceFinished;

        #endregion

        #region --- Properties ---

        public int LapAmount { get => _lapAmount; }
        public bool IsRacing { get; private set; }

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
        public void CallRestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        public void CallFinishRace()
        {
            IsRacing = false;
            OnRaceFinished?.Invoke();
        }

        private async UniTask StartRace()
        {
            OnRaceStarting?.Invoke(_delayBeforeStartRace);
            await UniTask.WaitForSeconds(_delayBeforeStartRace);

            IsRacing = true;
            OnRaceStarted?.Invoke();
        }

        #endregion
    }
}