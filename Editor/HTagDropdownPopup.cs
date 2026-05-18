using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    public class HTagDropdownPopup : PopupWindowContent
    {
        private string _searchString = "";
        private readonly SerializedProperty _tagsProperty;
        private Vector2 _scrollPosition;
        private readonly HashSet<BaseHTagSo> _selectedTags;
        private readonly bool _isMultiple;
        private readonly HTagAssetNode _rootNode;
        private static Color _defaultBackgroundColor = GUI.backgroundColor;

        public HTagDropdownPopup(SerializedProperty tagsProperty, Dictionary<string, BaseHTagSo> allTags, bool isMultiple)
        {
            _tagsProperty = tagsProperty;
            _isMultiple = isMultiple;
            _selectedTags = new HashSet<BaseHTagSo>();

            _rootNode = new HTagAssetNode("");
            foreach (var tag in allTags.Values)
            {
                _rootNode.TryAdd(new HTagAssetNode(tag));
            }

            if (_isMultiple)
            {
                for (int i = 0; i < _tagsProperty.arraySize; i++)
                {
                    var element = _tagsProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue is BaseHTagSo tag)
                    {
                        _selectedTags.Add(tag);
                        _rootNode.Find(tag.name).MakeFoldoutInHierarchy(true);
                    }
                }
            }
            else
            {
                if (_tagsProperty.objectReferenceValue is BaseHTagSo tag)
                {
                    _selectedTags.Add(tag);
                    _rootNode.Find(tag.name).MakeFoldoutInHierarchy(true);
                }
            }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 400);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchString = EditorGUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                _searchString = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var node in _rootNode.children.Values)
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
            
            bool wasChecked = _selectedTags.Contains(node.HTag);
            GUI.backgroundColor = wasChecked ? Color.darkCyan : _defaultBackgroundColor;
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.ToggleLeft("", wasChecked, GUILayout.Width(15));
            if (EditorGUI.EndChangeCheck())
            {
                if (_isMultiple)
                {
                    if (newValue)
                    {
                        _selectedTags.Add(node.HTag);
                    }
                    else
                    {
                        _selectedTags.Remove(node.HTag);
                    }
                }
                else
                {
                    _selectedTags.Clear();
                    _selectedTags.Add(node.HTag);
                }
                
                UpdateProperty();

                if (!_isMultiple)
                {
                    editorWindow.Close();
                }
            }
            
            if (node.children.Count > 0)
            {
                node.isFoldoutExpanded = EditorGUILayout.Foldout(node.isFoldoutExpanded, node.Name, true);
            }
            else
            {
                EditorGUILayout.LabelField(node.Name);
            }
            
            GUI.backgroundColor = _defaultBackgroundColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            if (node.isFoldoutExpanded || !string.IsNullOrEmpty(_searchString))
            {
                foreach (var child in node.children.Values)
                {
                    DrawNode(child, indent + 1);
                }
            }
        }

        private bool IsNodeVisible(HTagAssetNode node, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;
            if (node.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
            return node.children.Any(c => IsNodeVisible(c.Value, search));
        }

        private void UpdateProperty()
        {
            _tagsProperty.serializedObject.Update();

            if (_isMultiple)
            {
                _tagsProperty.ClearArray();
                _tagsProperty.arraySize = _selectedTags.Count;

                var sortedTags = _selectedTags.OrderBy(t => t.name).ToList();
                for (int i = 0; i < sortedTags.Count; i++)
                {
                    _tagsProperty.GetArrayElementAtIndex(i).objectReferenceValue = sortedTags[i];
                }
            }
            else
            {
                _tagsProperty.objectReferenceValue = _selectedTags.FirstOrDefault();
            }

            _tagsProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}