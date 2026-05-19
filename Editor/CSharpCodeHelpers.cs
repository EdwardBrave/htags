using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HTags.Editor
{
    internal static class CSharpCodeHelpers
    {
        private static readonly Dictionary<Type, Dictionary<string, BaseHTagSo>> AllTagsPairsForTypes = new ();
        
        internal static Dictionary<string, BaseHTagSo> GetAllTagsPairsIfValid(Type type)
        {
            if (type == null || type.IsAbstract)
            {
                return null;
            }
            
            if (AllTagsPairsForTypes.TryGetValue(type, out var cachedPairs))
            {
                return cachedPairs;
            }
            
            Type internalType = type;
            if (type.IsArray)
            {
                internalType = type.GetElementType();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                internalType = type.GetGenericArguments()[0];
            }

            if (internalType == null)
            {
                return null;
            }

            var hTagAsset = AssetDatabase.LoadAssetByGUID<HTagAsset>(AssetDatabase.FindAssetGUIDs($"t:{internalType.Name}").FirstOrDefault());
            if (!hTagAsset)
            {
                return null;
            }
            
            return AllTagsPairsForTypes[type] = hTagAsset.Tags
                    .OrderBy(so => so.name)
                    .ToDictionary(so => so.name, so => so);
        }
        
        
        public static BaseHTagSo CreateHTagField(HTagAsset parent, string tagName, int tagID = -1)
        {
            var options = parent.CodeGenerationOptions;
            options.namespaceName = MakeNamespaceName(options.namespaceName);
            options.tagName = MakeTypeName(Path.GetFileNameWithoutExtension(string.IsNullOrWhiteSpace(options.tagName) ? parent.name : options.tagName));

            var typeName = string.IsNullOrWhiteSpace(options.namespaceName) ? options.tagName : $"{options.namespaceName}.{options.tagName}";
            Type hTagType = Type.GetType($"{typeName}So, Assembly-CSharp");
            
            var child = ScriptableObject.CreateInstance(hTagType) as BaseHTagSo;
            child.name = tagName;
            child.tagID = tagID;
            AssetDatabase.AddObjectToAsset(child,parent);
            parent.Tags.Add(child);
            
            EditorUtility.SetDirty(parent);
            return child;
        }

        public static string ValidateFolderPath(string path, string defaultPath = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("Assets"))
                {
                    return path;
                }
                
                if (path.StartsWith(Application.dataPath))
                {
                    return Path.Combine("Assets", path.Substring(Application.dataPath.Length));
                }
            }
            
            Debug.LogWarning($"Selected folder is outside of the project's Assets directory or invalid. Using default path: {defaultPath}");
            return defaultPath;
        }
        
        public static List<string> GetValidatedListOfTags(IEnumerable<BaseHTagSo> tags)
        {
            return GetValidatedListOfTagNames(tags.Select(tag => tag.name));
        }
        
        public static List<string> GetValidatedListOfTagNames(IEnumerable<string> tagNames)
        {
            return tagNames
                .SelectMany(GetTagHierarchy)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();
        } 
        
        public static int[] GetTagHierarchyIDs(string fullTag, List<string> validatedTagNames)
        {
            return GetTagHierarchy(fullTag).Select(tagName => validatedTagNames.IndexOf(tagName)).ToArray();
        }
        
        public static string[] GetTagHierarchy(string fullTag)
        {
            fullTag = ValidateTagName(fullTag);
            List<string> tags = new List<string> { fullTag };

            while (fullTag.Contains('.')) 
            {
                fullTag = fullTag.Substring(0, fullTag.LastIndexOf('.'));
                tags.Add(fullTag);
            }
            
            return tags.ToArray();
        }
        
        public static string ValidateTagName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            
            name = name.Trim();
            var names = name.Split('.');
            if (names.All(IsProperIdentifier))
            {
                return name;
            }
            
            var builder = new StringBuilder();
            builder.AppendJoin('.', names.Select(str => MakeIdentifier(str)));
            return builder.ToString();
        }
        
        public static string WithAllWhitespaceStripped(this string str)
        {
            var buffer = new StringBuilder();
            foreach (var ch in str)
            {
                if (!char.IsWhiteSpace(ch))
                {
                    buffer.Append(ch);
                }
            }
            return buffer.ToString();
        }
        
        public static bool CheckOut(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Make path relative to project folder.
            var projectPath = Application.dataPath;
            if (path.StartsWith(projectPath) && path.Length > projectPath.Length &&
                (path[projectPath.Length] == '/' || path[projectPath.Length] == '\\'))
            {
                path = path.Substring(0, projectPath.Length + 1);
            }

            return AssetDatabase.MakeEditable(path);
        }
        
        public static bool IsProperIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (char.IsDigit(name[0]))
                return false;

            for (var i = 0; i < name.Length; ++i)
            {
                var ch = name[i];
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                    return false;
            }

            return true;
        }

        public static bool IsEmptyOrProperIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;

            return IsProperIdentifier(name);
        }

        public static bool IsEmptyOrProperNamespaceName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;

            return name.Split('.').All(IsProperIdentifier);
        }
        
        public static string MakeNamespaceName(string name)
        {
            if (IsEmptyOrProperNamespaceName(name))
                return name;
            
            var builder = new StringBuilder();
            builder.AppendJoin('.', name.Split('.').Select(str => MakeIdentifier(str)));
            return builder.ToString();
        }
        
        public static string MakeIdentifier(string name, string suffix = "")
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (char.IsDigit(name[0]))
                name = "_" + name;

            // See if we have invalid characters in the name.
            var nameHasInvalidCharacters = false;
            for (var i = 0; i < name.Length; ++i)
            {
                var ch = name[i];
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                {
                    nameHasInvalidCharacters = true;
                    break;
                }
            }

            // If so, create a new string where we remove them.
            if (nameHasInvalidCharacters)
            {
                var buffer = new StringBuilder();
                for (var i = 0; i < name.Length; ++i)
                {
                    var ch = name[i];
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                        buffer.Append(ch);
                }

                name = buffer.ToString();
            }

            return name + suffix;
        }

        public static string MakeTypeName(string name, string suffix = "")
        {
            var symbolName = MakeIdentifier(name, suffix);
            if (char.IsLower(symbolName[0]))
                symbolName = char.ToUpperInvariant(symbolName[0]) + symbolName.Substring(1);
            return symbolName;
        }

        public static string MakeAutoGeneratedCodeHeader(string toolName, string toolVersion, string sourceFileName = null)
        {
            return
                "//------------------------------------------------------------------------------\n"
                + "// <auto-generated>\n"
                + $"//     This code was auto-generated by {toolName}\n"
                + $"//     version {toolVersion}\n"
                + (string.IsNullOrEmpty(sourceFileName) ? "" : $"//     from {sourceFileName}\n")
                + "//\n"
                + "//     Changes to this file may cause incorrect behavior and will be lost if\n"
                + "//     the code is regenerated.\n"
                + "// </auto-generated>\n"
                + "//------------------------------------------------------------------------------\n";
        }

        public static string ToLiteral(this object value)
        {
            if (value == null)
                return "null";

            var type = value.GetType();

            if (type == typeof(bool))
            {
                if ((bool)value)
                    return "true";
                return "false";
            }

            if (type == typeof(char))
                return $"'\\u{(int)(char)value:X2}'";

            if (type == typeof(float))
                return value + "f";

            if (type == typeof(uint) || type == typeof(ulong))
                return value + "u";

            if (type == typeof(long))
                return value + "l";

            if (type.IsEnum)
            {
                var enumValue = type.GetEnumName(value);
                if (!string.IsNullOrEmpty(enumValue))
                    return $"{type.FullName.Replace("+", ".")}.{enumValue}";
            }

            return value.ToString();
        }
    }
}


