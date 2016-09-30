﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Oldmansoft.ClassicDomain.Driver.Mongo.Core
{
    internal abstract class DbSet<TDomain, TKey> : IDbSet<TDomain, TKey>
    {
        private MongoDatabase Database { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        protected string TableName { get; private set; }

        private ChangeList<TDomain> List { get; set; }

        private ConcurrentQueue<Func<MongoCollection<TDomain>, bool>> ExecuteList { get; set; }

        /// <summary>
        /// 主键表达式
        /// </summary>
        protected System.Linq.Expressions.Expression<Func<TDomain, TKey>> KeyExpression { get; set; }

        protected Func<TDomain, TKey> KeyExpressionCompile { get; set; }

        /// <summary>
        /// 创建实体集
        /// </summary>
        /// <param name="database"></param>
        /// <param name="keyExpression"></param>
        public DbSet(MongoDatabase database, System.Linq.Expressions.Expression<Func<TDomain, TKey>> keyExpression)
        {
            Database = database;
            TableName = typeof(TDomain).Name;
            List = new ChangeList<TDomain>();
            ExecuteList = new ConcurrentQueue<Func<MongoCollection<TDomain>, bool>>();
            KeyExpression = keyExpression;
            KeyExpressionCompile = keyExpression.Compile();
        }

        /// <summary>
        /// 注册移除
        /// </summary>
        /// <param name="domain"></param>
        void IDbSet<TDomain, TKey>.RegisterRemove(TDomain domain)
        {
            List.Deleteds.Enqueue(domain);
        }

        /// <summary>
        /// 注册替换
        /// </summary>
        /// <param name="domain"></param>
        void IDbSet<TDomain, TKey>.RegisterReplace(TDomain domain)
        {
            List.Updateds.Enqueue(domain);
        }

        /// <summary>
        /// 注册添加
        /// </summary>
        /// <param name="domain"></param>
        void IDbSet<TDomain, TKey>.RegisterAdd(TDomain domain)
        {
            List.Addeds.Enqueue(domain);
        }

        /// <summary>
        /// 注册执行
        /// </summary>
        /// <param name="execute"></param>
        void IDbSet<TDomain, TKey>.RegisterExecute(Func<MongoCollection<TDomain>, bool> execute)
        {
            ExecuteList.Enqueue(execute);
        }

        /// <summary>
        /// 获取 Mongo 集
        /// </summary>
        /// <returns></returns>
        public MongoCollection<TDomain> GetCollection()
        {
            return Database.GetCollection<TDomain>(TableName);
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        /// <returns></returns>
        string IDbSet<TDomain, TKey>.GetTableName()
        {
            return TableName;
        }

        /// <summary>
        /// 设置表名
        /// </summary>
        /// <param name="tableName"></param>
        void IDbSet<TDomain, TKey>.SetTableName(string tableName)
        {
            TableName = tableName;
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <returns></returns>
        public IQueryable<TDomain> Query()
        {
            return GetCollection().AsQueryable();
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TDomain Get(TKey id)
        {
            return GetCollection().FindOneById(Library.Extend.ToBsonValue(id));
        }

        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public int Commit()
        {
            var collection = GetCollection();

            int result = 0;
            TDomain domain;
            List<TDomain> addeds = new List<TDomain>();
            while (List.Addeds.TryDequeue(out domain))
            {
                addeds.Add(domain);
                result++;
            }
            if (addeds.Count > 0)
            {
                collection.InsertBatch(addeds);
            }

            while (List.Updateds.TryDequeue(out domain))
            {
                if (Replace(collection, domain)) result++;
            }
            while (List.Deleteds.TryDequeue(out domain))
            {
                if (Remove(collection, KeyExpressionCompile(domain))) result++;
            }

            Func<MongoCollection<TDomain>, bool> execute;
            while (ExecuteList.TryDequeue(out execute))
            {
                if (execute(collection)) result++;
            }
            return result;
        }
        
        /// <summary>
        /// 移除数据
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual bool Remove(MongoCollection<TDomain> collection, TKey id)
        {
            var query = MongoDB.Driver.Builders.Query<TDomain>.EQ(KeyExpression, id);
            var writeResult = collection.Remove(query);
            if (writeResult == null) return true;
            return writeResult.DocumentsAffected > 0;
        }

        /// <summary>
        /// 替换数据
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract bool Replace(MongoCollection<TDomain> collection, TDomain entity);
    }
}