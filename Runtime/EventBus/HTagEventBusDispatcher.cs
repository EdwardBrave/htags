using UnityEngine;
using UnityEngine.Events;

namespace HTags.EventBus
{
    public class HTagEventBusDispatcher : MonoBehaviour
    {
        [SerializeField]
        private BaseHTagSo hTagSo;
        
        public UnityEvent onTagEvent = new ();

        public void Raise()
        {
            HTagEventBus.Raise(hTagSo.BaseTag);
        }
        
        private void OnRaised()
        {
            onTagEvent.Invoke();
        }
        
        private void OnEnable()
        {
            HTagEventBus.AddListener(hTagSo.BaseTag, OnRaised);
        }
        
        private void OnDisable()
        {
            HTagEventBus.RemoveListener(hTagSo.BaseTag, OnRaised);
        }
    }
}