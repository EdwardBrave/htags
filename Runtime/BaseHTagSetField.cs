using System;

namespace HTags
{
    [Serializable]
    public abstract class BaseHTagSetField
    {
        public abstract Type HTagFieldType {  get; }
    }
}