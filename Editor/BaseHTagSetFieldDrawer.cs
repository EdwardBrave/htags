using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    [CustomPropertyDrawer(typeof(BaseHTagSetField), true)]
    public class BaseHTagSetFieldDrawer : PropertyDrawer
    {
        private Vector2 _scrollPosition;
        private HTagAssetNode _rootNode;
        private static GUIStyle _buttonStyle = new GUIStyle(EditorStyles.popup)
        {
            stretchHeight = true,
            fixedHeight = 0,
            alignment = TextAnchor.MiddleLeft
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.boxedValue is not BaseHTagSetField targetField ||
                property.FindPropertyRelative("tags") is not SerializedProperty tagsProp)
            {
                return base.GetPropertyHeight(property, label);
            }

            return GetButtonHeight(tagsProp.arraySize);
        }

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
            
            var allTagsPairs = CSharpCodeHelpers.GetAllTagsPairsIfValid(targetField.HTagFieldType);
            if (allTagsPairs == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

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
            
            var dropdownRect = new Rect(position.x, position.y, position.width, GetButtonHeight(tagsProp.arraySize));
            if (GUI.Button(dropdownRect, GetButtonText(tagsProp), _buttonStyle))
            {
                PopupWindow.Show(position, new HTagDropdownPopup(tagsProp, allTagsPairs, true));
            }
            
            EditorGUI.EndProperty();
        }

        private float GetButtonHeight(int tagsCount)
        {
            if (tagsCount <= 0) tagsCount = 1;
            return EditorGUIUtility.singleLineHeight * tagsCount - EditorGUIUtility.standardVerticalSpacing * 1.5f * (tagsCount - 1);
        }

        private string GetButtonText(SerializedProperty tagsProp)
        {
            if (tagsProp.arraySize == 0)
            {
                return "None";
            }

            var builder = new StringBuilder();

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var so = tagsProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (i < tagsProp.arraySize - 1)
                {
                    builder.AppendLine(so.name);
                }
                else
                {
                    builder.Append(so.name);
                }
            }

            return builder.ToString();
        }
    }
}