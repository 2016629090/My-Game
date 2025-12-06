using UnityEngine;

namespace YY.RPGgame
{
    public class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        protected static T m_instance;
        public static T instance
        {
            get
            {
                if (!m_instance)
                {
                    m_instance = FindAnyObjectByType<T>();
                }

                return m_instance;
            }
        }

        protected bool m_initialized;

        protected virtual void Initialize() { }

        protected virtual void Awake()
        {
            if(instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else if (!m_initialized)
            {
                Initialize();
                m_initialized = true;
            }
        }
    }
}
