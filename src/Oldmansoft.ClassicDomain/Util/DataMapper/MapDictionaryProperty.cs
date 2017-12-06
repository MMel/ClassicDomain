﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Oldmansoft.ClassicDomain.Util
{
    class MapDictionaryProperty : MapContentProperty
    {
        public override void Map(object source, ref object target)
        {
            var sourceValue = SourceProperty.GetValue(source);
            if (sourceValue == null)
            {
                TargetProperty.SetValue(target, null);
                return;
            }

            var currentSource = sourceValue as IDictionary;
            if (currentSource == null)
            {
                TargetProperty.SetValue(target, null);
                return;
            }

            var sourceKeyType = SourceType.GetGenericArguments()[0];
            var sourceValueType = SourceType.GetGenericArguments()[1];
            var targetKeyType = TargetType.GetGenericArguments()[0];
            var targetValueType = TargetType.GetGenericArguments()[1];

            var targetType = typeof(Dictionary<,>).MakeGenericType(targetKeyType, targetValueType);
            var isNormalClass = sourceValueType.IsNormalClass() && targetValueType.IsNormalClass();
            var targetValue = ObjectCreator.CreateInstance(targetType) as IDictionary;
            foreach (var key in currentSource.Keys)
            {
                targetValue.Add(key, DataMapper.ItemValueCopy(sourceValueType, targetValueType, isNormalClass, currentSource[key]));
            }
            TargetProperty.SetValue(target, targetValue);
        }
    }
}
