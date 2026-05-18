using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    [CustomEditor(typeof(HTagAsset))]
    public class HTagAssetEditor : UnityEditor.Editor
    {
        //--------------------------------------------------------------------------------------------------------------
    
        #region Data
        
        private enum EditNodeState
        {
            None,
            Add,
            Rename
        }

        private const string PrefCacheKeyConst = "HTags.Editor.HTagAssetEditor_Cache:";
        private static string MakePrefCacheKey(string guid) => PrefCacheKeyConst + guid;
        private string MakePrefCacheKey() => MakePrefCacheKey(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_targetAsset)));
        
        private SerializedProperty _optionsProp;
        private string _defaultAssetFolderPath;
        private SerializedProperty _tagFilesFolderPathProp;
        private SerializedProperty _tagNameProp;
        private SerializedProperty _namespaceNameProp;
        private SerializedProperty _registeredTagsProp;

        private string _searchString = "";
        private string _newTagName = "";

        private Vector2 _scrollPosition;

        private HTagAsset _targetAsset;

        private HTagAssetNode _rootNode;
        
        private static Color _defaultColor;

        private HTagAssetNode _activeEditNode;
        private string _activeEditName = "";
        private EditNodeState _editNodeState;
        private List<HTagAssetNode> _cachedAllNodes;
        private List<string> _cachedValidatedTagNames;

        private bool _isAssetValid = true;

        #endregion // Data
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Unity Events

        [InitializeOnLoadMethod]
        private static void HandleTagChangesAfterAssemblyReload()
        {
            var tagAssetsGUIDs = AssetDatabase.FindAssets("t:HTagAsset", new[] {"Assets"});
            var converter = new HTagAssetNodeJsonConverter();
            
            foreach (string tagAssetGUID in tagAssetsGUIDs)
            {
                string prefKey = MakePrefCacheKey(tagAssetGUID);
                if (!EditorPrefs.HasKey(prefKey))
                {
                    continue;
                }

                try
                {
                    string cachedData = EditorPrefs.GetString(prefKey);
                    converter.TagAsset = AssetDatabase.LoadAssetAtPath<HTagAsset>(AssetDatabase.GUIDToAssetPath(tagAssetGUID));;
                    var rootNode = JsonConvert.DeserializeObject<HTagAssetNode>(cachedData, converter);

                    if (rootNode != null && IsNodeTreeChanged(rootNode))
                    {
                        HandleTagChangesInAsset(rootNode, converter.TagAsset);
                    }
                }
                finally
                {
                    EditorPrefs.DeleteKey(prefKey);
                }
            }
        }
        
        private void OnEnable()
        {
            // Checking if asset is present in AssetDatabase
            string assetPath = AssetDatabase.GetAssetPath(target);
            _isAssetValid = !string.IsNullOrEmpty(assetPath);
            if (!_isAssetValid)
            {
                return;
            }
            
            AssemblyReloadEvents.afterAssemblyReload += RefreshRootNode;
            _defaultColor = GUI.contentColor;
            _defaultAssetFolderPath = Path.GetDirectoryName(assetPath);
            _targetAsset = (HTagAsset)target;
            _optionsProp = serializedObject.FindProperty("codeGenerationOptions");
            _tagFilesFolderPathProp = _optionsProp.FindPropertyRelative("tagFilesFolderPath");
            _tagNameProp = _optionsProp.FindPropertyRelative("tagName");
            _namespaceNameProp = _optionsProp.FindPropertyRelative("namespaceName");
            _registeredTagsProp = serializedObject.FindProperty("registeredTags");

            RefreshRootNode();
        }

        private void RefreshRootNode()
        {
            _rootNode = new HTagAssetNode("");
            foreach (var tag in _targetAsset.Tags)
            {
                _rootNode.TryAdd(new HTagAssetNode(tag));
            }
        }
        
        private void OnDisable()
        {
            if (!EditorPrefs.HasKey(MakePrefCacheKey()) && IsNodeTreeChanged(_rootNode) && 
                EditorUtility.DisplayDialog("Unsaved Changes", "Do you want to save changes before closing?",
                    "✔ Yes", "❌ No"))  
            {
                HandleTagChanges();
            }
            
            AssemblyReloadEvents.afterAssemblyReload -= RefreshRootNode;
            _targetAsset = null;
            _optionsProp = null;
            _tagFilesFolderPathProp = null;
            _tagNameProp = null;
            _namespaceNameProp = null;
            _registeredTagsProp = null;
            
            _rootNode = null;
        }

        public override void OnInspectorGUI()
        {
            if (!_isAssetValid)
            {
                EditorGUILayout.HelpBox("This asset is not valid for editing...", MessageType.None);
                return;
            }
            
            serializedObject.Update();

            DrawCodeGenerationOptionsSection();
            EditorGUILayout.Space();
            
            DrawAddTagSection();
            EditorGUILayout.Space();

            DrawHierarchySection();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion // Data
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Code Generation Options Section

        private void DrawCodeGenerationOptionsSection()
        {
            EditorGUILayout.LabelField("Code Generation Options", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (string.IsNullOrEmpty(_tagFilesFolderPathProp.stringValue))
            {
                _tagFilesFolderPathProp.stringValue = _defaultAssetFolderPath;
            }
            
            EditorGUILayout.PropertyField(_tagFilesFolderPathProp, new GUIContent("Tag Files Folder Path"));
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.SaveFolderPanel("Select Tags Scripts Folder Path", 
                    string.IsNullOrEmpty(_tagFilesFolderPathProp.stringValue) ? AssetDatabase.GetAssetPath(target) : _tagFilesFolderPathProp.stringValue, 
                    "");

                _tagFilesFolderPathProp.stringValue = CSharpCodeHelpers.ValidateFolderPath(path, _defaultAssetFolderPath);
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(_tagNameProp.stringValue))
            {
                _tagNameProp.stringValue = target.name;
            }
            EditorGUILayout.PropertyField(_tagNameProp, new GUIContent("Tag Name"));

            EditorGUILayout.PropertyField(_namespaceNameProp);
            
            if (GUILayout.Button("Generate code"))
            {
                HandleTagChanges();
            }
        }
        
        #endregion // Code Generation Options Section
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Add Tag Section

        private void DrawAddTagSection()
        {
            EditorGUILayout.BeginHorizontal();
            _newTagName = EditorGUILayout.TextField("New Tag Name", _newTagName);
            if (GUILayout.Button("Add Tag", GUILayout.Width(70)))
            {
                TryAddTag(_newTagName);
                _newTagName = "";
            }
            EditorGUILayout.EndHorizontal();
            
            DrawAutocomplete();
        }

        private void DrawAutocomplete()
        {
            if (string.IsNullOrWhiteSpace(_newTagName))
            {
                return;
            }

            var suggestions = _rootNode
                .Where(n => !n.IsRoot && n.FullName.Contains(_newTagName, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.FullName)
                .OrderBy(s => s.Length)
                .Take(5)
                .ToList();

            if (suggestions.Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            
            foreach (var suggestion in suggestions)
            {
                if (GUILayout.Button(suggestion, EditorStyles.helpBox))
                {
                    _newTagName = suggestion;
                    GUI.FocusControl(null);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion // Add Tag Section
        
        //--------------------------------------------------------------------------------------------------------------
    
        #region Hierarchy Section

        private void DrawHierarchySection()
        {
            EditorGUILayout.BeginHorizontal();
            _searchString = EditorGUILayout.TextField("Search", _searchString);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _searchString = "";
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);
            
            var nodes = _rootNode.children.Values.ToArray();
            foreach (var node in nodes)
            {
                DrawNode(node, 1);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(HTagAssetNode node, int indent)
        {
            bool matchSearch = string.IsNullOrEmpty(_searchString) || node.FullName.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
            bool hasVisibleChildren = node.children.Values.Any(c => IsNodeVisible(c, _searchString));
            
            if (!matchSearch && !hasVisibleChildren) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 15);
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            if (_editNodeState == EditNodeState.Rename && node == _activeEditNode)
            {
                DrawEditBox(newName => node.FullName = newName);
            }
            else
            {
                DrawRegularBox(node);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            if (node.isFoldoutExpanded || !string.IsNullOrEmpty(_searchString))
            {
                if (_editNodeState == EditNodeState.Add && node == _activeEditNode)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space((indent + 1) * 15);
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    DrawEditBox(newName => TryAddTag(node.FullName + "." + newName));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndHorizontal();
                }
                
                var nodes = node.children.Values.ToArray();
                foreach (var child in nodes)
                {
                    DrawNode(child, indent + 1);
                }
            }
        }
        
        private void DrawEditBox(Action<string> onConfirm, Action onCancel = null)
        {
            _activeEditName = EditorGUILayout.TextField(_activeEditName);
            if (GUILayout.Button("✅", GUILayout.Width(25)))
            {
                onConfirm?.Invoke(_activeEditName);
                _activeEditNode = null;
                _editNodeState = EditNodeState.None;
            }
            if (GUILayout.Button("❎", GUILayout.Width(25)))
            {
                onCancel?.Invoke();
                _activeEditNode = null;
                _editNodeState = EditNodeState.None;
            }
        }
        
        private void DrawRegularBox(HTagAssetNode node)
        {
            if ((node.Change & TagChange.RemovedMask) != 0)
            {
                GUI.contentColor = Color.softRed;
            }
            else if (node.Change.HasFlag(TagChange.Renamed))
            {
                GUI.contentColor = Color.yellow;
            }
            else if (node.Change.HasFlag(TagChange.New))
            {
                GUI.contentColor = Color.green;
            }
            
            if (node.children.Count > 0)
            {
                node.isFoldoutExpanded = EditorGUILayout.Foldout(node.isFoldoutExpanded, node.Name, true);
            }
            else
            {
                EditorGUILayout.LabelField(node.Name);
            }

            GUI.contentColor = _defaultColor;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                _activeEditNode = node;
                _activeEditName = "";
                _editNodeState = EditNodeState.Add;
                node.isFoldoutExpanded = true;
            }

            if (GUILayout.Button("🖊", GUILayout.Width(25)))
            {
                _activeEditNode = node;
                _activeEditName = node.FullName;
                _editNodeState = EditNodeState.Rename;
            }

            if ((node.Change & TagChange.RemovedMask) != 0)
            {
                if (GUILayout.Button("⤴️", GUILayout.Width(25)))
                {
                    node.MarkRemoval(false);
                }
            }
            else
            {
                if (GUILayout.Button("❌", GUILayout.Width(25)) &&
                    (node.children.Count == 0 || EditorUtility.DisplayDialog("Confirm?",
                        $"Are you sure you want to remove tag {node.FullName} with {node.children.Count} sub-tags?",
                        "Yes", "No")))
                {
                    node.MarkRemoval();
                }
            }
        }

        private bool IsNodeVisible(HTagAssetNode node, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;
            if (node.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
            return node.children.Any(c => IsNodeVisible(c.Value, search));
        }
        
        #endregion // Hierarchy Section
        
        //--------------------------------------------------------------------------------------------------------------

        #region Handle Tag Changes
        
        private static bool IsNodeTreeChanged(HTagAssetNode rootNode)
        {
            return rootNode.Any(n => !n.IsRoot && n.IsChanged);
        }
        
        private static List<string> GetValidTagNames(HTagAssetNode rootNode)
        {
            return CSharpCodeHelpers.GetValidatedListOfTagNames(
                rootNode
                    .Where(n => !n.IsRoot && (n.Change & TagChange.RemovedMask) == 0)
                    .Select(n => n.FullName));
        }
        
        private void TryAddTag(string tagName)
        {
            tagName = CSharpCodeHelpers.ValidateTagName(tagName);
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return;
            }
            
            _rootNode.TryAdd(new HTagAssetNode(tagName));
        }

        //Pre compile
        private void HandleTagChanges()
        {
            if (!IsNodeTreeChanged(_rootNode))
            {
                return;
            }
            
            string cachedData = JsonConvert.SerializeObject(_rootNode, new HTagAssetNodeJsonConverter{ TagAsset = _targetAsset});
            EditorPrefs.SetString(MakePrefCacheKey(), cachedData);
            
            HTagCodeGenerator.GenerateWrapperCode(_targetAsset, new HTagAsset.Options
            {
                tagFilesFolderPath = _tagFilesFolderPathProp.stringValue = 
                    CSharpCodeHelpers.ValidateFolderPath(_tagFilesFolderPathProp.stringValue, _defaultAssetFolderPath),
                tagName = _tagNameProp.stringValue = CSharpCodeHelpers.MakeIdentifier(_tagNameProp.stringValue),
                namespaceName = _namespaceNameProp.stringValue = CSharpCodeHelpers.MakeNamespaceName(_namespaceNameProp.stringValue),
            },
            GetValidTagNames(_rootNode));
        }
        
        //Post compile
        private static void HandleTagChangesInAsset(HTagAssetNode rootNode, HTagAsset asset)
        {
            var allNodes = rootNode.Where(n => !n.IsRoot).ToList();
            var validatedTagNames = GetValidTagNames(rootNode);
            
            for (int i = allNodes.Count - 1; i >= 0; --i)
            {
                var node = allNodes[i];
                node.isRefreshAllowed = false;
                
                if ((node.Change & TagChange.RemovedMask) != 0)
                {
                    if (node.HTag)
                    {
                        asset.Tags.Remove(node.HTag);
                        AssetDatabase.RemoveObjectFromAsset(node.HTag);
                    }
                    
                    node.RemoveSelf();
                    continue;
                }
                
                if (node.Change.HasFlag(TagChange.Renamed))
                {
                    node.HTag.name = node.FullName;
                }
                else if (node.Change.HasFlag(TagChange.New))
                {
                    node.HTag = CSharpCodeHelpers.CreateHTagField(asset, node.FullName);
                }
                
                node.HTag.tagIDs = CSharpCodeHelpers.GetTagHierarchyIDs(node.HTag.name, validatedTagNames);
                EditorUtility.SetDirty(node.HTag);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
        
        #endregion // Handle Tag Changes
        
        //--------------------------------------------------------------------------------------------------------------
    }
}
