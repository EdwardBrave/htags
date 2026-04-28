using UnityEngine;
using UnityEngine.Events;

namespace HTags.EventBus
{
    public class HTagEventBusDispatcher : MonoBehaviour
    {
        [SerializeField]
        private BaseHTagField hTagField;
        
        public UnityEvent onTagEvent = new ();
        
        private void Raise()
        {
            onTagEvent.Invoke();
        }
        
        private void OnEnable()
        {
            HTagEventBus.AddListener(hTagField.BaseTag, Raise);
        }
        
        private void OnDisable()
        {
            HTagEventBus.RemoveListener(hTagField.BaseTag, Raise);
        }
    }
}