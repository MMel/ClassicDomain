﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oldmansoft.ClassicDomain.Driver.Mongo
{
    /// <summary>
    /// 安全模式实体上下文
    /// 数据库连接串格式 mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
    /// 更多请参考 https://docs.mongodb.com/manual/reference/connection-string/
    /// </summary>
    public abstract class Context : Core.SafeModeContext, IContext
    {
    }

    /// <summary>
    /// 可传入初始化参数的实体上下文
    /// 数据库连接串格式 mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
    /// 更多请参考 https://docs.mongodb.com/manual/reference/connection-string/
    /// </summary>
    /// <typeparam name="TInit">初始化参数类型</typeparam>
    public abstract class Context<TInit> : Core.SafeModeContext, IContext<TInit>
    {
        /// <summary>
        /// 初始化方法，此方法由 UnitOfWork 调用
        /// </summary>
        /// <param name="parameter">初始化参数</param>
        public abstract void OnModelCreating(TInit parameter);

        /// <summary>
        /// 隐藏此方法
        /// </summary>
        public override void OnModelCreating()
        {
        }
    }
}
