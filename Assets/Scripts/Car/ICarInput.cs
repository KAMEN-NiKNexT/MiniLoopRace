namespace MiniRace
{
    public interface ICarInput
    {
        #region --- Properties ---

        public float ThrottleInput { get; }
        public float SteeringInput { get; }
        public bool IsHandbrakeActive { get; }

        #endregion

        #region --- Methods ---

        public void UpdateInput();

        #endregion
    }
}