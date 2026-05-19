using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTags
{
    [Serializable]
    [CreateAssetMenu(fileName = "new HTag Asset", menuName = "HTag/HTagAsset")]
    public class HTagAsset : ScriptableObject
    {
        [Serializable]
        public struct Options
        {
            public string tagFilesFolderPath;
            public string tagName;
            public string namespaceName;
        }
        
        [SerializeField] 
        private Options codeGenerationOptions;
        
        [Header( "Hierarchical tags list" )]
        [SerializeField] 
        private List<BaseHTagSo> registeredTags = new ();
        
        public Options CodeGenerationOptions => codeGenerationOptions;
        
        public List<BaseHTagSo> Tags => registeredTags;
    }
}
