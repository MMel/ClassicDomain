﻿using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Oldmansoft.ClassicDomain.Util;
using System;

namespace Oldmansoft.ClassicDomain.Driver.Mongo.Library
{
    /// <summary>
    /// 更新内容上下文
    /// </summary>
    internal class UpdateContext
    {
        /// <summary>
        /// 获取上下文
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <param name="compareSource"></param>
        /// <param name="compareTarget"></param>
        /// <returns></returns>
        public static UpdatedItem GetContext(object id, Type type, object compareSource, object compareTarget)
        {
            var result = new UpdatedItem(id.ToBsonValue());
            GetUpdateContext(type, result, new string[0], compareSource, compareTarget);
            return result;
        }

        private static void GetUpdateContext(Type type, UpdatedItem result, string[] names, object compareSource, object compareTarget)
        {
            foreach (var property in TypePublicInstancePropertyInfoStore.GetValues(type))
            {
                var propertyName = property.Name;
                if (propertyName.ToLower() == "id") propertyName = "_id";
                var currentNames = names.AddToNew(propertyName);

                var sourceValue = compareSource == null ? null : property.Get(compareSource);
                var targetValue = compareTarget == null ? null : property.Get(compareTarget);
                var propertyType = property.Type;
                if (sourceValue == null && targetValue == null) continue;

                if (propertyType.IsArrayOrGenericList())
                {
                    DealList(result, propertyType, currentNames, sourceValue, targetValue);
                    continue;
                }

                if (propertyType.IsGenericDictionary())
                {
                    DealDictionary(result, propertyType, currentNames, sourceValue, targetValue);
                    continue;
                }

                if (propertyType.IsNormalClass())
                {
                    DealNormalClass(result, propertyType, currentNames, sourceValue, targetValue);
                    continue;
                }

                if (!sourceValue.IsEquals(targetValue))
                {
                    result.Add(Update.Set(currentNames.JoinDot(), targetValue.ToBsonValue()));
                }
            }
        }

        private static void DealNormalClass(UpdatedItem result, Type type, string[] names, object sourceValue, object targetValue)
        {
            if (sourceValue != null && targetValue != null)
            {
                GetUpdateContext(type, result, names, sourceValue, targetValue);
                return;
            }
            result.Add(new UpdateBuilder().SetObjectWrapped(names.JoinDot(), targetValue));
        }

        private static void DealList(UpdatedItem result, Type propertyType, string[] names, object sourceValue, object targetValue)
        {
            if (sourceValue != null && targetValue != null)
            {
                DealListChange(result, propertyType, names, sourceValue, targetValue);
                return;
            }
            result.Add(new UpdateBuilder().SetObjectWrapped(names.JoinDot(), targetValue));
        }

        private static void DealListChange(UpdatedItem result, Type propertyType, string[] names, object sourceValue, object targetValue)
        {
            var itemType = propertyType.IsArray ? propertyType.GetMethod("Set").GetParameters()[1].ParameterType : propertyType.GetGenericArguments()[0];
            var isNormalClass = itemType.IsNormalClass();
            var sourceList = sourceValue as System.Collections.IList;
            var targetList = targetValue as System.Collections.IList;
            var name = names.JoinDot();

            if (IsByteArray(propertyType, itemType))
            {
                if (IsByteArrayContentDifferent(sourceList, targetList))
                {
                    result.Add(new UpdateBuilder().SetObjectWrapped(name, targetValue));
                }
                return;
            }

            for (var i = 0; i < targetList.Count; i++)
            {
                if (i >= sourceList.Count)
                {
                    result.AddOther(Update.Push(name, GetBsonValue(itemType, targetList[i], isNormalClass)));
                    continue;
                }
                if (isNormalClass)
                {
                    DealNormalClass(result, itemType, names.AddToNew(i.ToString()), sourceList[i], targetList[i]);
                    continue;
                }
                if (!sourceList[i].IsEquals(targetList[i]))
                {
                    result.Add(Update.Set(GetListKey(names, i), GetBsonValue(itemType, targetList[i])));
                }
            }

            var isRemoveObject = false;
            for (var i = targetList.Count; i < sourceList.Count; i++)
            {
                isRemoveObject = true;
                result.AddOther(new UpdateBuilder().SetObjectWrapped(GetListKey(names, i), RemoveObject.Instance));
            }
            if (isRemoveObject) result.AddOther(new UpdateBuilder().PullObjectWrapped(name, RemoveObject.Instance));
        }

        private static void DealDictionary(UpdatedItem result, Type propertyType, string[] names, object sourceValue, object targetValue)
        {
            if (sourceValue != null && targetValue != null)
            {
                DealDictionaryChange(result, propertyType, names, sourceValue, targetValue);
                return;
            }
            result.Add(new UpdateBuilder().SetObjectWrapped(names.JoinDot(), targetValue));
        }

        private static void DealDictionaryChange(UpdatedItem result, Type propertyType, string[] names, object sourceValue, object targetValue)
        {
            var valueType = propertyType.GetGenericArguments()[1];
            var isNormalClass = valueType.IsNormalClass();
            var sourceDictionary = sourceValue as System.Collections.IDictionary;
            var targetDictionary = targetValue as System.Collections.IDictionary;

            foreach (var key in targetDictionary.Keys)
            {
                if (!sourceDictionary.Contains(key))
                {
                    result.Add(Update.Set(GetHashKey(names, key.ToString()), GetBsonValue(valueType, targetDictionary[key], isNormalClass)));
                    continue;
                }
                if (isNormalClass)
                {
                    DealNormalClass(result, valueType, names.AddToNew(key.ToString()), sourceDictionary[key], targetDictionary[key]);
                    continue;
                }
                if (!sourceDictionary[key].IsEquals(targetDictionary[key]))
                {
                    result.Add(Update.Set(GetHashKey(names, key.ToString()), GetBsonValue(valueType, targetDictionary[key], isNormalClass)));
                }
            }
            foreach (var key in sourceDictionary.Keys)
            {
                if (!targetDictionary.Contains(key))
                {
                    result.AddOther(Update.Unset(GetHashKey(names, key.ToString())));
                }
            }
        }

        private static bool IsByteArray(Type propertyType, Type itemType)
        {
            return propertyType.IsArray && itemType == typeof(byte);
        }

        private static bool IsByteArrayContentDifferent(System.Collections.IList sourceList, System.Collections.IList targetList)
        {
            if (targetList.Count != sourceList.Count) return true;
            for (var i = 0; i < targetList.Count; i++)
            {
                if ((byte)sourceList[i] != (byte)targetList[i])
                {
                    return true;
                }
            }
            return false;
        }

        private static BsonValue GetBsonValue(Type type, object value, bool isNormalClass = false)
        {
            if (isNormalClass)
            {
                return BsonDocumentWrapper.Create(type, value);
            }
            return value.ToBsonValue();
        }

        private static string GetHashKey(string[] source, string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException(string.Format("属性 {0} 的键不允许为空", source.JoinDot()));
            if (key.IndexOf('.') > -1) throw new ArgumentException(string.Format("属性 {0} 的键不允许有字符“.”", source.JoinDot()));
            if (key.IndexOf('$') > -1) throw new ArgumentException(string.Format("属性 {0} 的键不允许有字符“$”", source.JoinDot()));
            return source.AddToNew(key).JoinDot();
        }

        private static string GetListKey(string[] source, int index)
        {
            return source.AddToNew(index.ToString()).JoinDot();
        }
    }
}
