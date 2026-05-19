using Unity.Collections;
using UnityEngine;

namespace HTags
{
    public interface IHTag
    {
        public int RegisteredTagsCount { get; }
        public NativeArray<int> TagIDs { get; }
    }
    
    public abstract class BaseHTagSo : ScriptableObject
    {
        public int tagID;

        public NativeArray<int> TagIDs => BaseTag.TagIDs;
        
        public abstract IHTag BaseTag { get; }
    }
}