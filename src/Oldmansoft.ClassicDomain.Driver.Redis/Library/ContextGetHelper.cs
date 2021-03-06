﻿using Oldmansoft.ClassicDomain.Util;
using System;
using System.Collections.Generic;

namespace Oldmansoft.ClassicDomain.Driver.Redis.Library
{
    class ContextGetHelper
    {
        public static ReflectionItem GetReflection(Type type)
        {
            var result = new ReflectionItem();
            SetReflection(type, result, new string[0]);
            return result;
        }

        private static void SetReflection(Type type, ReflectionItem result, string[] prefixNames)
        {
            foreach (var property in TypePublicInstancePropertyInfoStore.GetPropertys(type))
            {
                var currentNames = prefixNames.AddToNew(property.Name);
                var propertyType = property.PropertyType;
                if (propertyType.IsArrayOrGenericList())
                {
                    result.ListNames.Add(currentNames.JoinDot());
                    continue;
                }

                if (propertyType.IsGenericDictionary())
                {
                    result.HashNames.Add(currentNames.JoinDot());
                    continue;
                }

                if (propertyType.IsNormalClass())
                {
                    SetReflection(propertyType, result, currentNames);
                    continue;
                }
            }
        }

        public static T GetContext<T>(DataGetMapping mapping) where T : class, new()
        {
            if (mapping == null || mapping.Fields.Count == 0) return default;
            var result = new T();
            SetContext(mapping, typeof(T), result, new string[0]);
            return result;
        }

        private static void SetContext<T>(DataGetMapping mapping, Type type, T instance, string[] prefixNames)
        {
            foreach (var property in TypePublicInstancePropertyInfoStore.GetValues(type))
            {
                var currentNames = prefixNames.AddToNew(property.Name);
                var name = currentNames.JoinDot();
                var propertyType = property.Type;
                if (propertyType.IsArray)
                {
                    SetArray(mapping, instance, property, name);
                    continue;
                }

                if (propertyType.IsGenericDictionary())
                {
                    SetDictionary(mapping, instance, property, name);
                    continue;
                }

                if (propertyType.IsGenericList())
                {
                    SetList(mapping, instance, property, name);
                    continue;
                }

                if (!mapping.Fields.ContainsKey(name)) continue;
                if (propertyType.IsNormalClass())
                {
                    var obj = ObjectCreator.CreateInstance(propertyType);
                    SetContext(mapping, propertyType, obj, currentNames);
                    property.Set(instance, obj);
                    continue;
                }

                var value = mapping.Fields[name];
                property.Set(instance, propertyType.FromString(value));
            }
        }

        private static void SetArray(DataGetMapping mapping, object instance, IValue property, string name)
        {
            if (!mapping.Lists.ContainsKey(name)) return;
            property.Set(instance, mapping.Lists[name].GetArrayFromString(property.Type));
        }

        private static void SetDictionary(DataGetMapping mapping, object instance, IValue property, string name)
        {
            if (!mapping.Hashs.ContainsKey(name)) return;

            var propertyType = property.Type;
            var keyType = propertyType.GetGenericArguments()[0];
            var valueType = propertyType.GetGenericArguments()[1];
            var isKeyNormalClass = keyType.IsNormalClass();
            var isValueNormalClass = valueType.IsNormalClass();
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            if (propertyType == dictionaryType)
            {
                var dictionary = ObjectCreator.CreateInstance(dictionaryType) as System.Collections.IDictionary;
                foreach (var item in mapping.Hashs[name])
                {
                    dictionary.Add(item.Key.GetValueFromString(keyType, isKeyNormalClass), item.Value.GetValueFromString(valueType, isValueNormalClass));
                }
                property.Set(instance, dictionary);
                return;
            }

            throw new NotSupportedException(string.Format("不支持 {0} 类型序列化", propertyType.FullName));
        }

        private static void SetList(DataGetMapping mapping, object instance, IValue property, string name)
        {
            if (!mapping.Lists.ContainsKey(name)) return;

            var propertyType = property.Type;
            var itemType = propertyType.GetEnumerableItemType();
            var listType = typeof(List<>).MakeGenericType(itemType);
            if (propertyType == listType)
            {
                property.Set(instance, mapping.Lists[name].GetListFromString(listType, itemType));
                return;
            }

            var queueType = typeof(Queue<>).MakeGenericType(itemType);
            if (propertyType == queueType)
            {
                property.Set(instance, Activator.CreateInstance(queueType, mapping.Lists[name].GetListFromString(listType, itemType)));
                return;
            }

            var stackType = typeof(Stack<>).MakeGenericType(itemType);
            if (propertyType == stackType)
            {
                property.Set(instance, Activator.CreateInstance(stackType, mapping.Lists[name].GetListFromString(listType, itemType)));
                return;
            }

            throw new NotSupportedException(string.Format("不支持 {0} 类型序列化", propertyType.FullName));
        }
    }
}
