using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MiniRace.Control
{
    public class GameManager : SingletonComponent<GameManager>
    {
        #region --- Members ---

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
        }

        #endregion

        #region --- Control Methods ---

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