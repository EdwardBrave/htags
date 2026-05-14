using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    [CustomPropertyDrawer(typeof(BaseHTagSo), true)]
    public class BaseHTagFieldDrawer : PropertyDrawer
    {
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var allTagsPairs = CSharpCodeHelpers.GetAllTagsPairsIfValid(fieldInfo.FieldType);
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