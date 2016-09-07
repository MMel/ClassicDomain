﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oldmansoft.ClassicDomain.Util;

namespace Oldmansoft.ClassicDomain.Driver.Mongo
{
    /// <summary>
    /// 快速模式实体上下文
    /// </summary>
    public abstract class FastModeContext : UnitOfWorkManagedItem, IContext
    {
        private static FastModeConfig FastServer { get; set; }

        static FastModeContext()
        {
            FastServer = new FastModeConfig();
        }

        private Dictionary<Type, IDbSet> DbSet { get; set; }

        /// <summary>
        /// 创建 Mongo 的实体上下文
        /// </summary>
        public FastModeContext()
        {
            DbSet = new Dictionary<Type, IDbSet>();
        }

        /// <summary>
        /// 创建实体集
        /// </summary>
        /// <typeparam name="TDomain"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keyExpression"></param>
        /// <returns></returns>
        internal virtual FastModeDbSet<TDomain, TKey> CreateDbSet<TDomain, TKey>(System.Linq.Expressions.Expression<Func<TDomain, TKey>> keyExpression)
        {
            return new FastModeDbSet<TDomain, TKey>(FastServer.Get(ConnectionName).GetDatabase(), keyExpression);
        }

        /// <summary>
        /// 添加领域上下文
        /// 主键表达式必须为 Id
        /// </summary>
        /// <typeparam name="TDomain">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <param name="keyExpression">主键表达式</param>
        /// <returns>设置</returns>
        public Setting<TDomain, TKey> Add<TDomain, TKey>(System.Linq.Expressions.Expression<Func<TDomain, TKey>> keyExpression)
        {
            Type type = typeof(TDomain);
            if (DbSet.ContainsKey(type))
            {
                throw new ArgumentException("已添加了此实体类型。");
            }
            if (keyExpression == null)
            {
                throw new ArgumentNullException("keyExpression");
            }
            if (keyExpression.GetProperty().Name != "Id")
            {
                throw new ArgumentException("主键表达式必须为 Id");
            }
            var dbSet = CreateDbSet(keyExpression);
            DbSet.Add(type, dbSet);
            return new Setting<TDomain, TKey>(dbSet);
        }

        IDbSet<TDomain, TKey> IContext.Set<TDomain, TKey>()
        {
            Type type = typeof(TDomain);
            if (!DbSet.ContainsKey(type))
            {
                throw new ClassicDomainException(string.Format("{0} 类型没有添加到 {1} 上下文中。", type.FullName, this.GetType().FullName));
            }
            var result = DbSet[type] as FastModeDbSet<TDomain, TKey>;
            if (result == null)
            {
                throw new ClassicDomainException("Set 获取的主键类型与 Add 添加的主键类型不一致。");
            }
            return result;
        }

        /// <summary>
        /// 获取 commit 的主机
        /// </summary>
        /// <returns></returns>
        public override string GetHost()
        {
            return FastServer.Get(ConnectionName).GetHost();
        }

        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public override int Commit()
        {
            try
            {
                int result = 0;
                foreach (IDbSet item in DbSet.Values)
                {
                    result += item.Commit();
                }
                return result;
            }
            catch (MongoDB.Driver.MongoDuplicateKeyException ex)
            {
                throw new UniqueException(ex);
            }
        }
    }

    /// <summary>
    /// 快速模式实体上下文
    /// </summary>
    /// <typeparam name="TInit"></typeparam>
    public abstract class FastModeContext<TInit> : FastModeContext, IContext<TInit>
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
