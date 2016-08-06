﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oldmansoft.ClassicDomain
{
    /// <summary>
    /// 加载，添加
    /// </summary>
    /// <typeparam name="TDomain"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRepositoryLoadAdd<TDomain, TKey> :
        ILoad<TDomain, TKey>,
        IAdd<TDomain>
        where TDomain : class
    {
    }
}
