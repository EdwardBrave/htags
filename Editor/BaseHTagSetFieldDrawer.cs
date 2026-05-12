using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    [CustomPropertyDrawer(typeof(BaseHTagSetField), true)]
    public class BaseHTagSetFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.boxedValue is not BaseHTagSetField targetField)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var tagsProp = property.FindPropertyRelative("tags");
            if (tagsProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Error: 'tags' field not found.");
                return;
            }

            var tagFieldType = targetField.HTagFieldType;
            if (tagFieldType == null)
            {
                EditorGUI.LabelField(position, label.text, "Error: HTagFieldType is null.");
                return;
            }

            var hTagAsset = AssetDatabase.LoadAssetByGUID<HTagAsset>(AssetDatabase.FindAssetGUIDs($"t:{tagFieldType}").FirstOrDefault());
            if (!hTagAsset)
            {
                EditorGUI.LabelField(position, label.text, $"Error: No HTagAsset found for type '{tagFieldType.Name}'.");
                return;
            }
            
            var allTagsPairs = hTagAsset.Tags
                .Select(tagField => new KeyValuePair<string, BaseHTagSo>(tagField.name, tagField))
                .OrderBy(pair => pair.Key)
                .ToArray();

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            for (int i = tagsProp.arraySize - 1; i >= 0; --i)
            {
                if (tagsProp.GetArrayElementAtIndex(i).objectReferenceValue is BaseHTagSo tagField &&
                    allTagsPairs.Any(pair => pair.Value == tagField))
                {
                    continue;
                }
                
                tagsProp.DeleteArrayElementAtIndex(i);
            }
            
            var dropdownRect = new Rect(position.x, position.y, position.width, position.height);
            if (EditorGUI.DropdownButton(dropdownRect, GetButtonGUIContent(tagsProp), FocusType.Keyboard, EditorStyles.popup))
            {
                PopupWindow.Show(dropdownRect, new HTagDropdownPopup(tagsProp, allTagsPairs, true));
            }
            
            EditorGUI.EndProperty();
        }

        private GUIContent GetButtonGUIContent(SerializedProperty tagsProp)
        {
            if (tagsProp.arraySize == 0)
            {
                return new GUIContent("None");
            }

            var rootNode = new HTagAssetNode("");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var so = tagsProp.GetArrayElementAtIndex(i).objectReferenceValue;
                rootNode.TryAdd(new HTagAssetNode(so.name));
            }

            return new GUIContent(rootNode.ToStringTree());
        }
    }
}