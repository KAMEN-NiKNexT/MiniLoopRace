using UnityEngine;

namespace Car
{
    /// <summary>
    /// Управление эффектами автомобиля (дым, следы, звуки)
    /// </summary>
    public class CarEffectsManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Particle Effects")]
        [SerializeField] private bool _useEffects = true;
        [SerializeField] private ParticleSystem _rearLeftWheelParticleSystem;
        [SerializeField] private ParticleSystem _rearRightWheelParticleSystem;

        [Header("Tire Skid Effects")]
        [SerializeField] private TrailRenderer _rearLeftWheelTireSkid;
        [SerializeField] private TrailRenderer _rearRightWheelTireSkid;

        [Header("Sounds")]
        [SerializeField] private bool _useSounds = true;
        [SerializeField] private AudioSource _carEngineSound;
        [SerializeField] private AudioSource _tireScreechSound;

        [Header("UI")]
        [SerializeField] private bool _useUI = false;
        [SerializeField] private UnityEngine.UI.Text _carSpeedText;

        #endregion

        #region Private Fields

        private float _initialCarEngineSoundPitch;

        #endregion

        #region Unity Methods

        private void Start()
        {
            if (_carEngineSound != null)
            {
                _initialCarEngineSoundPitch = _carEngineSound.pitch;
            }

            DisableEffectsIfNeeded();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обновление эффектов заноса
        /// </summary>
        public void UpdateDriftingEffects(bool isDrifting, bool isTractionLocked, float carSpeed)
        {
            if (!_useEffects)
            {
                return;
            }

            // Управление дымом из-под колес
            if (isDrifting)
            {
                if (_rearLeftWheelParticleSystem != null && !_rearLeftWheelParticleSystem.isPlaying)
                {
                    _rearLeftWheelParticleSystem.Play();
                }
                if (_rearRightWheelParticleSystem != null && !_rearRightWheelParticleSystem.isPlaying)
                {
                    _rearRightWheelParticleSystem.Play();
                }
            }
            else
            {
                if (_rearLeftWheelParticleSystem != null && _rearLeftWheelParticleSystem.isPlaying)
                {
                    _rearLeftWheelParticleSystem.Stop();
                }
                if (_rearRightWheelParticleSystem != null && _rearRightWheelParticleSystem.isPlaying)
                {
                    _rearRightWheelParticleSystem.Stop();
                }
            }

            // Управление следами от колес
            bool shouldEmitTireSkids = (isTractionLocked || Mathf.Abs(isDrifting ? 10f : 0f) > 5f) && Mathf.Abs(carSpeed) > 12f;

            if (_rearLeftWheelTireSkid != null)
            {
                _rearLeftWheelTireSkid.emitting = shouldEmitTireSkids;
            }

            if (_rearRightWheelTireSkid != null)
            {
                _rearRightWheelTireSkid.emitting = shouldEmitTireSkids;
            }
        }

        /// <summary>
        /// Обновление звуков автомобиля
        /// </summary>
        public void UpdateCarSounds(float carSpeed, bool isDrifting, bool isTractionLocked)
        {
            if (!_useSounds)
            {
                return;
            }

            // Звук двигателя
            if (_carEngineSound != null)
            {
                float engineSoundPitch = _initialCarEngineSoundPitch + (Mathf.Abs(carSpeed) / 25f);
                _carEngineSound.pitch = engineSoundPitch;
            }

            // Звук скольжения шин
            if (_tireScreechSound != null)
            {
                if ((isDrifting) || (isTractionLocked && Mathf.Abs(carSpeed) > 12f))
                {
                    if (!_tireScreechSound.isPlaying)
                    {
                        _tireScreechSound.Play();
                    }
                }
                else
                {
                    if (_tireScreechSound.isPlaying)
                    {
                        _tireScreechSound.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Обновление UI отображения скорости
        /// </summary>
        public void UpdateSpeedUI(float carSpeed)
        {
            if (!_useUI || _carSpeedText == null)
            {
                return;
            }

            float absoluteCarSpeed = Mathf.Abs(carSpeed);
            _carSpeedText.text = Mathf.RoundToInt(absoluteCarSpeed).ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Отключение эффектов, если они не используются
        /// </summary>
        private void DisableEffectsIfNeeded()
        {
            if (!_useEffects)
            {
                if (_rearLeftWheelParticleSystem != null)
                {
                    _rearLeftWheelParticleSystem.Stop();
                }

                if (_rearRightWheelParticleSystem != null)
                {
                    _rearRightWheelParticleSystem.Stop();
                }

                if (_rearLeftWheelTireSkid != null)
                {
                    _rearLeftWheelTireSkid.emitting = false;
                }

                if (_rearRightWheelTireSkid != null)
                {
                    _rearRightWheelTireSkid.emitting = false;
                }
            }

            if (!_useSounds)
            {
                if (_carEngineSound != null)
                {
                    _carEngineSound.Stop();
                }

                if (_tireScreechSound != null)
                {
                    _tireScreechSound.Stop();
                }
            }
        }

        #endregion
    }
}