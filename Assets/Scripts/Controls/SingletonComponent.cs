using UnityEngine;

namespace MiniRace.Control
{
    public abstract class SingletonComponent<T> : MonoBehaviour where T : Object
    {
        #region Member Variables

        private static T instance;
        private static bool _isInitialized = false;

        #endregion

        #region Properties

        public static T Instance
        {
            get
            {
                if (!_isInitialized)
                {
                    instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

                    if (instance != null) _isInitialized = true;
                    else Debug.LogError($"[SingletonComponent] Object \"{typeof(T)}\" does not exist in the scene!");
                }
                return instance;
            }
        }

        #endregion

        #region Unity Methods

        protected virtual void Awake()
        {
            InitializeInstance();
        }

        #endregion

        #region Other Methods

        private void InitializeInstance()
        {
            instance = gameObject.GetComponent<T>();

            if (instance != null) _isInitialized = true;
        }

        #endregion
    }
}