using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    [CustomPropertyDrawer(typeof(BaseHTagSo), true)]
    public class BaseHTagFieldDrawer : PropertyDrawer
    {
        private Dictionary<Type, KeyValuePair<string, BaseHTagSo>[]> _allTagsPairsForTypes = new ();
        
        private KeyValuePair<string, BaseHTagSo>[] GetAllTagsPairsIfValid(Type type)
        {
            if (type.IsAbstract)
            {
                return null;
            }
            
            if (_allTagsPairsForTypes.TryGetValue(type, out var cachedPairs))
            {
                return cachedPairs;
            }
             
            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == null)
            {
                return null;
            }

            var hTagAsset = AssetDatabase.LoadAssetByGUID<HTagAsset>(AssetDatabase.FindAssetGUIDs($"t:{type.Name}").FirstOrDefault());
            if (!hTagAsset)
            {
                return null;
            }
            
            return _allTagsPairsForTypes[type] = hTagAsset.Tags
                .Select(tagField => new KeyValuePair<string, BaseHTagSo>(tagField.name, tagField))
                .OrderBy(pair => pair.Key)
                .ToArray();
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var allTagsPairs = GetAllTagsPairsIfValid(fieldInfo.FieldType);
            if (allTagsPairs == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var dropdownRect = new Rect(position.x, position.y, position.width, position.height);
            string buttonText = property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(buttonText), FocusType.Keyboard, EditorStyles.popup))
            {
                PopupWindow.Show(dropdownRect, new HTagDropdownPopup(property, allTagsPairs, false));
            }

            EditorGUI.EndProperty();
        }
    }
}