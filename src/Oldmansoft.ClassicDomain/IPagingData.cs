﻿namespace Oldmansoft.ClassicDomain
{
    /// <summary>
    /// 分页数据
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    public interface IPagingData<TSource>
    {
        /// <summary>
        /// 设置页大小
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        IPagingResult<TSource> Size(int value);
    }
}
