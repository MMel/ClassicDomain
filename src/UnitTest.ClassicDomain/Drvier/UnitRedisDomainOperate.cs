﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest.ClassicDomain.Drvier.Redis
{
    [TestClass]
    public class UnitRedisDomainOperate
    {
        [TestMethod]
        public void TestAddReplaceRemove()
        {
            var factory = new Factory();
            var repository = factory.CreateBook();

            var domain = new Domain.Book();
            domain.Name = "hello";
            domain.Authors = new System.Collections.Generic.List<Domain.Author>();
            domain.Tags = new System.Collections.Generic.List<string>();
            domain.Tags.Add("hello");
            repository.Add(domain);
            factory.GetUnitOfWork().Commit();

            domain = repository.Get(domain.Id);
            Assert.IsNotNull(domain);

            domain.Name = "world";
            repository.Replace(domain);
            factory.GetUnitOfWork().Commit();

            domain = repository.Get(domain.Id);
            Assert.AreEqual("world", domain.Name);

            repository.Remove(domain);
            factory.GetUnitOfWork().Commit();

            domain = repository.Get(domain.Id);
            Assert.IsNull(domain);
        }
    }
}