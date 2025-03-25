using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MiniRace.Game;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRace.Control
{
    public class UIManager : SingletonComponent<UIManager>
    {
        #region --- Enums ---

        public enum CanvasType
        {
            Menu,
            Game,
            Finish
        }

        #endregion

        #region --- Classes ---

        [Serializable]
        private class CanvasViewInfo
        {
            [SerializeField] private CanvasType _type;
            [SerializeField] private Canvas _canvas;

            public CanvasType Type { get => _type; }
            public Canvas Canvas { get => _canvas; }
        }

        #endregion

        #region --- Members ---

        [Header("Objects")]
        [SerializeField] private CanvasViewInfo[] _canvasInfos;
        [SerializeField] private Button _startRaceButton;
        [SerializeField] private Button _restartRace;
        [SerializeField] private TextMeshProUGUI _startText;
        [Space]
        [SerializeField] private TextMeshProUGUI _lapValue;
        [SerializeField] private TextMeshProUGUI _placeValue;
        [SerializeField] private TextMeshProUGUI _finishPlaceValue;
        [SerializeField] private TextMeshProUGUI _speedValue;
        [SerializeField] private TextMeshProUGUI[] _timerValue;

        [Space]
        [SerializeField] private RaceCarController _playerCar;

        #endregion

        #region --- Mono Override Methods ---

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }
        private void Update()
        {
            _speedValue.text = Mathf.RoundToInt(_playerCar.LocalVelocityZ * 4).ToString();
        }

        #endregion

        #region --- Control Methods ---

        private void Initialize()
        {
            _startRaceButton.onClick.AddListener(() => GameManager.Instance.CallStartRace());
            _restartRace.onClick.AddListener(() => GameManager.Instance.CallRestartScene());

            GameManager.Instance.OnRaceStarting += CallStarting;
            GameManager.Instance.OnRaceFinished += () => ShowCanvas(CanvasType.Finish);

            RacePositionManager.Instance.OnCarsPositionUpdated += UpdatePlaceUI;
            TimerManager.Instance.OnTimerChanged += UpdateTimerUI;

            ShowCanvas(CanvasType.Menu);
        }
        private void CallStarting(int value)
        {
            _canvasInfos.First((info) => info.Type == CanvasType.Menu).Canvas.gameObject.SetActive(false);
            TimerToStart(value).Forget();
        }

        private async UniTask TimerToStart(int timerValue)
        {
            _startText.gameObject.SetActive(true);
            for (int i = timerValue - 1; i >= 1; i--)
            {
                _startText.text = $"{i}";
                await UniTask.WaitForSeconds(1);
            }

            _startText.text = "START";
            await UniTask.WaitForSeconds(1);
            _startText.gameObject.SetActive(false);

            ShowCanvas(CanvasType.Game);
        }
        private void ShowCanvas(CanvasType canvasType)
        {
            for (int i = 0; i < _canvasInfos.Length; i++)
            {
                _canvasInfos[i].Canvas.gameObject.SetActive(_canvasInfos[i].Type == canvasType);
            }
        }
        private void UpdatePlaceUI(List<ICarPositionTracker> places)
        {
            if (!GameManager.Instance.IsRacing) return;

            _placeValue.text = $"{places.FindIndex(car => car.IsPlayer) + 1}/{places.Count}";
            _finishPlaceValue.text = $"{places.FindIndex(car => car.IsPlayer) + 1} place";
            _lapValue.text = $"{places.First(car => car.IsPlayer).CurrentLap}/{GameManager.Instance.LapAmount}";
        }
        private void UpdateTimerUI(string value)
        {
            for (int i = 0; i < _timerValue.Length; i++)
            {
                _timerValue[i].text = value;
            }
        }

        #endregion
    }
}