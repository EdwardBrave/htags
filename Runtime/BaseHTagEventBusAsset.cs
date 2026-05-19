using System;
using UnityEngine;

namespace HTags
{
    [Serializable]
    public abstract class BaseHTagEventBusAsset : ScriptableObject
    {
        [SerializeField]
        public HTagAsset hTagAsset;
    }
}