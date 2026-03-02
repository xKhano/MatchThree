using UnityEngine;

namespace DefaultNamespace
{
    public class EventBus : MonoBehaviour
    {
        public static EventBus Instance { get; private set; }
        
    }
}