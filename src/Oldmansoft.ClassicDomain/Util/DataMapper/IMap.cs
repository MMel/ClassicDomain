﻿namespace Oldmansoft.ClassicDomain.Util
{
    /// <summary>
    /// 映射接口
    /// </summary>
    public interface IMap
    {
        /// <summary>
        /// 映射
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        void Map(object source, object target);
    }
}
