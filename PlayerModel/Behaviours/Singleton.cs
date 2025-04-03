using UnityEngine;

namespace PlayerModel.Behaviours
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; protected set; }

        public static bool HasInstance => Instance;

        public virtual bool SingleInstance { get; } = true; // defeats the purpose of a "singleton" but still nice to have a crutial instance you can point to that most behaviours inherit

        private T GenericComponent => gameObject.GetComponent<T>();

        public void Awake()
        {
            if (SingleInstance && HasInstance && Instance != GenericComponent)
            {
                Destroy(GenericComponent);
            }
            else if (SingleInstance || !HasInstance)
            {
                Instance = GenericComponent;
            }

            Initialize();
        }

        public virtual void Initialize()
        {
            //Logging.Info($"Initializing singleton for {typeof(T).Name}");
        }
    }
}
