using Unity.Collections;
using UnityEngine;

namespace HTags
{
    public interface IHTag
    {
        public int AllTagsCount { get; }
        public NativeArray<int> TagIDs { get; }
    }
    
    public abstract class BaseHTagSo : ScriptableObject
    {
        public int[] tagIDs;

        public abstract IHTag BaseTag { get; }
    }
}