ClassicDomain C# Driver
=================

You can get the latest stable release from the [official Nuget.org feed](https://www.nuget.org/profiles/Oldman).


Getting Started
---------------

### MongoDB Sample
You need to download [MongoDB](https://www.mongodb.com/download-center).

#### web.config or app.config
```Xml
<connectionStrings>
	<add name="Sample.Repositories.Mapping" connectionString="mongodb://localhost/Sample?j=true" />
</connectionStrings>
```

#### Domain
```C#
namespace Sample.Domain
{
	public class Person
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}
}
```

#### Repositories
```C#
namespace Sample.Repositories
{
	class Mapping : Oldmansoft.ClassicDomain.Driver.Mongo.Context
	{
		protected override void OnModelCreating()
		{
			Add<Domain.Person, Guid>(o => o.Id).SetUnique(o => o.Name);
		}
	}
}
```

```C#
using Oldmansoft.ClassicDomain;
namespace Sample.Repositories
{
	public class RepositoryFactory
	{
		private UnitOfWork Uow { get; set; }

		 public RepositoryFactory()
		{
			Uow = new UnitOfWork();
		}

		public IUnitOfWork GetUnitOfWork()
		{
			return Uow;
		}

		public IRepository<Domain.Person, Guid> CreatePerson()
		{
			return new Oldmansoft.ClassicDomain.Driver.Mongo.Repository<Domain.Person, Guid, Mapping>(Uow);
		}
	}
}
```

#### Add Domain To Repository
```C#
using Oldmansoft.ClassicDomain;
namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			var factory = new Repositories.RepositoryFactory();
			var repository = factory.CreatePerson();
			var domain = new Domain.Person();
			domain.Name = "Oldman";
			repository.Add(domain);
			factory.GetUnitOfWork().Commit();
		}
	}
}
```

#### Modify Domain From Repository
```C#
using Oldmansoft.ClassicDomain;
namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			var factory = new Repositories.RepositoryFactory();
			var repository = factory.CreatePerson();
			var domain = repository.Query().FirstOrDefault(o => o.Name == "Oldman");
			domain.Name = "Oldmansoft";
			repository.Replace(domain);
			factory.GetUnitOfWork().Commit();
		}
	}
}
```

#### Remove Domain From Repository
```C#
using Oldmansoft.ClassicDomain;
namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			var factory = new Repositories.RepositoryFactory();
			var repository = factory.CreatePerson();
			var domain = repository.Query().FirstOrDefault(o => o.Name == "Oldmansoft");
			repository.Remove(domain);
			factory.GetUnitOfWork().Commit();
		}
	}
}
```

### Maintainers:
* Oldman                    https://github.com/Oldmansoft

If you have contributed and we have neglected to add you to this list please contact one of the maintainers to be added to the list (with apologies).
