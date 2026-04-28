using UnityEngine;
using UnityEngine.Events;

namespace HTags.EventBus
{
    public class HTagEventBusDispatcher : MonoBehaviour
    {
        [SerializeField]
        private BaseHTagField hTagField;
        
        public UnityEvent onTagEvent = new ();

        public void Raise()
        {
            HTagEventBus.Raise(hTagField.BaseTag);
        }
        
        private void OnRaised()
        {
            onTagEvent.Invoke();
        }
        
        private void OnEnable()
        {
            HTagEventBus.AddListener(hTagField.BaseTag, OnRaised);
        }
        
        private void OnDisable()
        {
            HTagEventBus.RemoveListener(hTagField.BaseTag, OnRaised);
        }
    }
}