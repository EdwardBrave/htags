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
        private readonly KeyValuePair<string, BaseHTagField>[] _allTags;
        private Vector2 _scrollPosition;
        private readonly HashSet<BaseHTagField> _selectedTags;
        private readonly bool _isMultiple;

        private static readonly GUIStyle ElementStyle = new (EditorStyles.toolbarButton)
        {
            alignment = TextAnchor.MiddleLeft
        };

        public HTagDropdownPopup(SerializedProperty tagsProperty, KeyValuePair<string, BaseHTagField>[] allTags, bool isMultiple)
        {
            _tagsProperty = tagsProperty;
            _allTags = allTags;
            _isMultiple = isMultiple;
            _selectedTags = new HashSet<BaseHTagField>();

            if (_isMultiple)
            {
                for (int i = 0; i < _tagsProperty.arraySize; i++)
                {
                    var element = _tagsProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue is BaseHTagField tag)
                    {
                        _selectedTags.Add(tag);
                    }
                }
            }
            else
            {
                if (_tagsProperty.objectReferenceValue is BaseHTagField tag)
                {
                    _selectedTags.Add(tag);
                }
            }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(250, 300);
        }

        public override void OnGUI(Rect rect)
        {
            _searchString = EditorGUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var pair in _allTags)
            {
                if (string.IsNullOrEmpty(_searchString) || pair.Key.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                {
                    bool isChecked = _selectedTags.Contains(pair.Value);

                    if (_isMultiple)
                    {
                        EditorGUI.BeginChangeCheck();
                        bool newValue = EditorGUILayout.ToggleLeft(pair.Key, isChecked, ElementStyle);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (newValue)
                            {
                                _selectedTags.Add(pair.Value);
                            }
                            else
                            {
                                _selectedTags.Remove(pair.Value);
                            }
                            UpdateProperty();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(pair.Key, ElementStyle))
                        {
                            _selectedTags.Clear();
                            _selectedTags.Add(pair.Value);
                            UpdateProperty();
                            this.editorWindow.Close();
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
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