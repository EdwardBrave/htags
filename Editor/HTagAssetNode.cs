using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HTags.Editor
{
    [Flags]
    public enum TagChange
    {
        Unchanged = 0,
        New = 1 << 0,
        Renamed = 1 << 1,
        SelfRemoved = 1 << 2,
        ParentRemoved = 1 << 3,
        RemovedMask = SelfRemoved | ParentRemoved,
    }
    
    [Serializable]
    public class HTagAssetNode : IEnumerable<HTagAssetNode>
    {
        //--------------------------------------------------------------------------------------------------------------
    
        #region Public Data and Properties

        /// <summary>
        /// It is used to prevent refreshing cached instance after Assembly Reload, i.e., after deserialization
        /// since hierarchical references are not serialized properly
        /// </summary>
        public bool isRefreshAllowed = true;
        public bool isFoldoutExpanded;
        
        private string _cachedName;
        public string Name
        {
            get => _cachedName;
            set
            {
                parent?.children.Remove(Name);
                NewName = value;
                parent?.children.Add(Name, this);
                RefreshCachedValues();
            }
        }
        
        private string _cachedFullName;
        public string FullName
        {
            get => _cachedFullName;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || FullName == value)
                {
                    return;
                }
                var rootRef = Root;
                RemoveSelf();
                var newFullNameParts = value.Split('.');
                rootRef.TryAdd(this, newFullNameParts);
                RefreshCachedValues();
            }
        }
        
        #endregion // Public Data and Properties
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Readonly Data, Properties and Caching
        
        [SerializeField]
        public readonly SortedList<string, HTagAssetNode> children = new ();

        [SerializeField]
        private HTagAssetNode parent;
        public HTagAssetNode Parent
        {
            get => parent;
            private set
            {
                if (value == parent)
                {
                    return;
                }
                parent?.children.Remove(Name);
                parent = value;
                parent?.children.Add(Name, this);
                RefreshCachedValues();
            }
        }
        
        [SerializeField]
        private BaseHTagSo hTag;
        public BaseHTagSo HTag
        {
            get => hTag;
            internal set
            {
                if (value == hTag)
                {
                    return;
                }
                
                hTag = value;
                RefreshCachedValues();
            }
        }

        public bool IsRoot => parent == null;
        public HTagAssetNode Root { get; private set; }
        
        private string NewName { get; set; }
        
        public TagChange Change { get; internal set; }
        public bool IsChanged => Change != TagChange.Unchanged;
        
        private void RefreshCachedValues()
        {
            if (!isRefreshAllowed)
            {
                return;
            }
            
            if (IsRoot)
            {
                Root = this;
                _cachedFullName = _cachedName = string.IsNullOrWhiteSpace(NewName) ? HTag?.name : NewName;
            }
            else
            {
                Root = parent.Root;
                _cachedName = (string.IsNullOrWhiteSpace(NewName) ? HTag?.name : NewName)?.Split('.').Last();
                _cachedFullName = string.IsNullOrWhiteSpace(parent.FullName) ? _cachedName : $"{parent.FullName}.{_cachedName}";
            }
            
            RefreshTagChange();
            if (IsChanged)
            {
                isFoldoutExpanded = true;
                var parentNode = Parent;
                while (parentNode != null)
                {
                    parentNode.isFoldoutExpanded = true;
                    parentNode = parentNode.Parent;
                }
            }

            foreach (var childNode in children.Values)
            {
                childNode.RefreshCachedValues();
            }
        }

        private void RefreshTagChange()
        {
            Change &= TagChange.RemovedMask;
            
            if (HTag == null)
            {
                Change |= TagChange.New;
            }
            else if (!string.IsNullOrWhiteSpace(NewName) && HTag.name != FullName)
            {
                Change |= TagChange.Renamed;
            }
        }
        
        #endregion // Data and Properties
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Constructors and public interface
        
        public HTagAssetNode(BaseHTagSo baseHTag, string newFullName = null)
        {
            hTag = baseHTag;
            NewName = newFullName;
            RefreshCachedValues();
        }
        
        public HTagAssetNode(string newFullName)
        {
            NewName = newFullName;
            RefreshCachedValues();
        }
        
        
        public bool TryAdd(HTagAssetNode newNode)
        {   
            var newFullNameParts = newNode.FullName.Split('.');
            return Root.TryAdd(newNode, newFullNameParts);
        }
        
        public HTagAssetNode Find(string fullName)
        {
            var splitItems = fullName.Split('.');
            return Root.Find(splitItems);
        }
        
        public void RemoveSelf()
        {
            Parent = null;
        }

        public void MarkRemoval(bool isToRemove = true)
        { 
            if (isToRemove)
            {
                Change |= TagChange.SelfRemoved;
                var childNodes = this.ToArray();
                for (int i = childNodes.Length - 1; i >= 0; --i)
                {
                    var childNode = childNodes[i];
                    childNode.Change |= TagChange.ParentRemoved;
                    if (childNode.children.Count == 0 && childNode.Change.HasFlag(TagChange.New))
                    {
                        childNode.RemoveSelf();
                    }
                }

                return;
            }
            
            Change &= ~TagChange.RemovedMask;
            
            foreach (var childNode in this)
            {
                childNode.Change &= ~TagChange.ParentRemoved;
            }
            
            var parentNode = Parent;
            while (parentNode != null)
            {
                parentNode.Change &= ~TagChange.RemovedMask;
                parentNode = parentNode.Parent;
            }
        }

        public override string ToString()
        {
            switch (Change)
            {
                case TagChange.SelfRemoved or TagChange.ParentRemoved:
                    return $"#{FullName}";
                case TagChange.Renamed:
                    return $"{HTag?.name} -> *{FullName}";
                case TagChange.New:
                    return $"*{FullName}";
                default:
                    return FullName;
            }
        }

        public string ToStringTree()
        {
            var sb = new StringBuilder();
            bool isNamelessRoot = IsRoot && string.IsNullOrEmpty(FullName);

            if (!isNamelessRoot)
            {
                sb.Append(Name);
            }
            
            if (children is { Count: > 0 })
            {
                if (!isNamelessRoot)
                {
                    sb.Append("{");
                }
                
                sb.AppendJoin(", ", children.Values.Select(child => child.ToStringTree()));
                
                if (!isNamelessRoot)
                {
                    sb.Append("}");
                }
            }
            
            return sb.ToString();
        }

        #endregion // Constructors and public interface
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Private implementation
        
        private bool TryAdd(HTagAssetNode newNode, in string[] fullNameParts, int depthIndex = 0)
        {
            if (depthIndex >= fullNameParts.Length)
            {
                Debug.LogError($"Unexpected case! Index out of bounds: {depthIndex} >= {fullNameParts.Length}");
                return false;
            }
            
            bool isNamelessRoot = string.IsNullOrEmpty(FullName);
            int nextDepthIndex = isNamelessRoot ? depthIndex : depthIndex + 1;
            
            if (!isNamelessRoot && depthIndex == fullNameParts.Length - 1 && Name == fullNameParts[depthIndex])
            {
                if (HTag == null && newNode.HTag != null)
                {
                    HTag = newNode.HTag;
                    return true;
                }
            
                Debug.LogError($"Trying to add a node with the same name as an existing node: {FullName}");
                return false;
            }
            
            if (children.TryGetValue(fullNameParts[nextDepthIndex], out var subNode))
            {
                return subNode.TryAdd(newNode, fullNameParts, nextDepthIndex);
            }
                
            var parentNode = this;
            for (int i = nextDepthIndex; i < fullNameParts.Length - 1; i++)
            {
                var middleNode = new HTagAssetNode(fullNameParts[i]);
                middleNode.Parent = parentNode;
                parentNode = middleNode;
            }
            
            newNode.Name = fullNameParts[^1];
            newNode.Parent = parentNode;

            return true;
        }
        
        private HTagAssetNode Find(string[] fullNameParts, int depthIndex = 0)
        {
            if (depthIndex >= fullNameParts.Length)
            {
                return null;
            }
            
            bool isNamelessRoot = string.IsNullOrEmpty(FullName);
            int nextDepthIndex = isNamelessRoot ? depthIndex : depthIndex + 1;

            if (!isNamelessRoot && depthIndex == fullNameParts.Length - 1 && Name == fullNameParts[depthIndex])
            {
                return this;
            }
            
            if (children.TryGetValue(fullNameParts[nextDepthIndex], out var subNode))
            {
                return subNode.Find(fullNameParts, nextDepthIndex);
            }
            
            return null;
        }
        
        #endregion // Private implementation
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region IEnumerable implementation

        public IEnumerator<HTagAssetNode> GetEnumerator()
        {
            return new InDepthEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class InDepthEnumerator : IEnumerator<HTagAssetNode>
        {
            public HTagAssetNode Current { get; private set; }

            object IEnumerator.Current => Current;
            
            private Stack<int> _childIndexes = new ();

            private readonly HTagAssetNode _initialNode;
            
            private bool _isReset = true;
            
            public InDepthEnumerator(HTagAssetNode current)
            {
                _initialNode = current;
            }

            public bool MoveNext()
            {
                if (_isReset)
                {
                    _isReset = false;
                    Current = _initialNode;
                    return true;
                }
                
                if (Current == null)
                {
                    return false;
                }

                if (Current.children.Count > 0)
                {
                    Current = Current.children.Values.ElementAt(0);
                    _childIndexes.Push(0);
                    return true;
                }

                Current = Current.Parent;
                
                while (Current != null && _childIndexes.Count > 0)
                {
                    var index = _childIndexes.Pop() + 1;
                    if (index < Current.children.Count)
                    {
                        Current = Current.children.Values.ElementAt(index);
                        _childIndexes.Push(index);
                        return true;
                    }
                    
                    Current = Current.Parent;
                }

                return false;
            }

            public void Reset()
            {
                _isReset = true;
                Current = null;
                _childIndexes.Clear();
            }

            public void Dispose()
            {
                Current = null;
                _childIndexes.Clear();
            }
        }
        
        #endregion // IEnumerable implementation
        
        //--------------------------------------------------------------------------------------------------------------
    }
}