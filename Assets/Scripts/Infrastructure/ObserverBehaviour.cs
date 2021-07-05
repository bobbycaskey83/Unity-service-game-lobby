using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample
{
    /// <summary>
    /// Observes an Observed class, intitializes with Observed State when beginning observation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObserverBehaviour<T> : MonoBehaviour where T : Observed<T>
    {
        public T observed { get; set; }
        
        public UnityEvent<T> OnObservedUpdated;

        /// <summary>
        /// Option to allow certain observers to not be registered by the GameStateManager automatically.
        /// </summary>
        public bool observeOnStart = true;

        protected virtual void UpdateObserver(T obs)
        {
            observed = obs;
            OnObservedUpdated?.Invoke(observed);
        }

        public void BeginObserving(T target)
        {
            if (target == null)
            {
                Debug.LogError($"Needs a Target of type {typeof(T)} to begin observing.", gameObject);
                return;
            }
          
            UpdateObserver(target);
            observed.onChanged += UpdateObserver;
        }

        public void EndObserving()
        {
            if (observed == null)
                return;
            if (observed.onChanged != null)
                observed.onChanged -= UpdateObserver;
            observed = null;
        }

        void Awake()
        {
            if (observed == null)
                return;
            BeginObserving(observed);
        }

        void OnDestroy()
        {
            if (observed == null)
                return;
            EndObserving();
        }
    }
}
