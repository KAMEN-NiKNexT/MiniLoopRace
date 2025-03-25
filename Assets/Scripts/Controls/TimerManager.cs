using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MiniRace.Control
{
    public class TimerManager : SingletonComponent<TimerManager>
    {
        #region --- Members --- 

        [Header("Variables")]
        private int _totalSeconds;
        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region --- Events ---

        public event Action<string> OnTimerChanged;

        #endregion

        #region --- Mono Override Methods ---

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        #endregion

        #region --- Control Methods ---

        private void Initialize()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            GameManager.Instance.OnRaceStarted += ActivateTimer;
            GameManager.Instance.OnRaceFinished += StopTimer;
        }

        private void ActivateTimer()
        {
            StartTimer(_cancellationTokenSource.Token).Forget();
        }

        private void StopTimer()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async UniTask StartTimer(CancellationToken cancellationToken)
        {
            _totalSeconds = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.WaitForSeconds(1, cancellationToken: cancellationToken);
                _totalSeconds++;
                UpdateTimerText();
            }
        }

        private void UpdateTimerText()
        {
            int minutes = _totalSeconds / 60;
            int seconds = _totalSeconds % 60;
            OnTimerChanged?.Invoke($"{minutes:00}:{seconds:00}");
        }

        #endregion
    }
}