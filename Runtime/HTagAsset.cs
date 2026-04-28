using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTags
{
    [Serializable]
    [CreateAssetMenu(fileName = "new HTag Asset", menuName = "HTagAsset")]
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
        
        [SerializeField]
        private bool autoGenerateOnClosing = true;
        
        [Header( "Hierarchical tags list" )]
        [SerializeField] 
        private List<BaseHTagField> registeredTags = new ();
        
        public Options CodeGenerationOptions => codeGenerationOptions;
        
        public List<BaseHTagField> Tags => registeredTags;
    }
}
