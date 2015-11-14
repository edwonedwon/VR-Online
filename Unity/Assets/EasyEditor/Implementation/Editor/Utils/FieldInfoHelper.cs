//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace EasyEditor
{
    /// <summary>
    /// Tool class providing many information about a field info contained into a monobehaviour/scriptableobject class. Ex : can the field be serialized by Unity ?
    /// What is the path of this field inside the monobehaviour/scriptableobject structure ?
    /// </summary>
	public class FieldInfoHelper {

        public static bool RenderedByDefaultInUnity(FieldInfo fieldInfo)
        {
            return IsSerializedInUnity(fieldInfo) && !IsHideInInspector(fieldInfo);
        }

        public static bool IsSerializedInUnity(FieldInfo fieldInfo)
        {
            bool isSerializedInUnity = (fieldInfo.IsPublic || HasSerializedFieldAttribute(fieldInfo))
                && !HasNotSerializedFieldAttribute(fieldInfo)
                && (
                IsSerializedTypeInUnity(fieldInfo.FieldType)
                || IsArrayOfSerializedInUnity(fieldInfo.FieldType)
                || IsListOfSerializedInUnity(fieldInfo.FieldType)
                );

            return isSerializedInUnity;
        }

        public static bool IsSerializedTypeInUnity(Type type)
        {
            return (
            (!type.FullName.StartsWith("System.") && HasSerializableAttribute(type) && (type.IsClass || type.IsValueType))
                || IsSerializedTypeByDefaultInUnity(type)
            );
        }

        public static bool IsSerializedTypeByDefaultInUnity(Type type)
        {
            return (
                type.IsEnum || typeof(UnityEngine.Object).IsAssignableFrom(type)
                || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)
                || type == typeof(Quaternion) || type == typeof(Matrix4x4) || type == typeof(Color)
                || type == typeof(Rect) || type == typeof(LayerMask) || type == typeof(Bounds)
                || type == typeof(int) || type == typeof(float) || type == typeof(bool) || type == typeof(string) || type == typeof(double)
                || type == typeof(byte)
                );
        }

        public static bool IsSerializedCustomClassOrStruct(Type type)
        {
            return (
                IsSerializedTypeInUnity(type) && !IsSerializedTypeByDefaultInUnity(type)
                );
        }

        public static bool IsArrayOfSerializedInUnity(Type type)
        {
            return type.IsArray && IsSerializedTypeInUnity(type.GetElementType());
        }

        public static bool IsListOfSerializedInUnity(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && IsSerializedTypeInUnity(type.GetGenericArguments()[0]);
        }

        public static bool IsArrayOfSerializedCustomClassOrStruct(Type type)
        {
            return type.IsArray && IsSerializedCustomClassOrStruct(type.GetElementType());
        }
        
        public static bool IsListOfSerializedCustomClassOrStruct(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && IsSerializedCustomClassOrStruct(type.GetGenericArguments()[0]);
        }

        public static bool HasSerializedFieldAttribute(FieldInfo fieldInfo)
        {
            Attribute attribute = AttributeHelper.GetAttribute<SerializeField>(fieldInfo);
            return (attribute != null);
        }

        public static bool HasSerializableAttribute(Type type)
        {
            object[] groups = type.GetCustomAttributes(typeof(SerializableAttribute), true);
            return groups.Length > 0;
        }

        public static bool HasNotSerializedFieldAttribute(FieldInfo fieldInfo)
        {
            NonSerializedAttribute attribute = AttributeHelper.GetAttribute<NonSerializedAttribute>(fieldInfo);
            return (attribute != null);
        }

        public static bool IsHideInInspector(FieldInfo fieldInfo)
        {
            HideInInspector attribute = AttributeHelper.GetAttribute<HideInInspector>(fieldInfo);
            return (attribute != null);
        }

        static public string GetFieldInfoPath(FieldInfo fieldInfo, Type typeSource)
        {
            string path = "";
            
            Type declaringType = fieldInfo.DeclaringType;
            if (declaringType == typeSource)
            {
                path = fieldInfo.Name; 
            }
            else
            {
                FieldInfo[] customClasses = GetCustomClasses(typeSource);
                
                if(customClasses.Length == 0)
                {
                    return "";
                }
                else
                {
                    foreach(FieldInfo field in customClasses)
                    {
                        path += field.Name + "." + GetFieldInfoPath(fieldInfo, field.FieldType);
                        if(path.Contains(fieldInfo.Name))
                        {
                            break;
                        }
                        else
                        {
                            path = "";
                        }
                    }
                }
            }
            
            return path;
        }

        public static object GetFieldReferenceFromName(string fieldName, BindingFlags bindingFlags, object objHolder)
        {
            object result = null;

            FieldInfo fieldInfo = objHolder.GetType ().GetField (fieldName, bindingFlags);
            
            if (fieldInfo != null)
            {
                result = fieldInfo.GetValue(objHolder);
            }
            
            return result;
        }

        /// <summary>
        /// Gets the custom class reference from the source object containing it. The custom class is 
        /// not necessarily a direct member of the source object.
        /// </summary>
        /// <returns>The custom class reference.</returns>
        /// <param name="fieldInfo">Field info representing the custom class.</param>
        /// <param name="sourceObject">Source object.</param>
        public static object GetCustomClassReference(FieldInfo fieldInfo, object sourceObject)
        {
            object result = null;

            string path = FieldInfoHelper.GetFieldInfoPath(fieldInfo, sourceObject.GetType());
            if (!string.IsNullOrEmpty(path))
            {
                result = sourceObject;
                string[] pathTable = path.Split('.');
                for (int i = 0; i<pathTable.Length; i++)
                {
                    result = FieldInfoHelper.GetFieldReferenceFromName(pathTable [i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, result);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets a field FieldInfo from the source object containing it based on the subclasses path (for field representing a custom serializable classes).
        /// </summary>
        /// <returns>The field info indicated by the path.</returns>
        /// <param name="path">Path from the sourceObject (ex : if source object has 
        /// a custom class bag with a field weight in it, weight path will be bag.weight.</param>
        /// <param name="sourceObject">Source object.</param>
        public static FieldInfo GetFieldInfoFromPath(string path, object sourceObject)
        {
            FieldInfo result = null;

            string[] pathTable = path.Split('.');
            if(pathTable.Length > 0)
            {
                result = sourceObject.GetType().GetField(pathTable[0]);
                for (int i = 1; i<pathTable.Length; i++)
                {
                    result = result.GetType().GetField(pathTable[i]);
                }
            }
            
            return result;
        }

        static public SerializedProperty GetSerializedPropertyFromPath(string path, SerializedObject serializedObject)
        {
            SerializedProperty result = null;

            if (!string.IsNullOrEmpty(path))
            {
                if(!path.Contains("."))
                {
                    result = serializedObject.FindProperty(path);
                }
                else
                {
                    int indexOfFirstPoint = path.IndexOf('.');
                    result = serializedObject.FindProperty(path.Substring(0, indexOfFirstPoint));
                    result = result.FindPropertyRelative(path.Substring(indexOfFirstPoint + 1));
                }
            }

            return result;
        }

        static public object GetObjectFromPath(string path, object sourceObject)
        {
            object result = null;

            string[] pathTable = path.Split('.');
            if (pathTable.Length > 0)
            {
                result = sourceObject.GetType().GetField(pathTable[0]).GetValue(sourceObject);
                for (int i = 1; i<pathTable.Length; i++)
                {
                    if(pathTable[i] == "Array")
                    {
                        int firstBracket = pathTable[i+1].IndexOf('[');
                        int secondBracket = pathTable[i+1].IndexOf(']');

                        string arrayIndex = pathTable[i+1].Substring(firstBracket + 1, secondBracket - firstBracket - 1);
                        result = ((IList) result)[int.Parse(arrayIndex)];
                        i += 2;
                    }
                    else
                    {
                        result = result.GetType().GetField(pathTable[i]).GetValue(result);
                    }
                }
            }

            return result;
        }

        static private FieldInfo[] GetCustomClasses(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            List<FieldInfo> result = new List<FieldInfo>();
            foreach (FieldInfo field in fields)
            {
                if(IsSerializedCustomClassOrStruct(field.FieldType) || IsListOfSerializedCustomClassOrStruct(field.FieldType) 
                   || IsArrayOfSerializedCustomClassOrStruct(field.FieldType))
                {
                    result.Add(field);
                }
            }
            
            return result.ToArray();
        }
	}
}