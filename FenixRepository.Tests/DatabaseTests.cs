using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FenixRepo.Context;
using FenixRepo.Context.Models;
using FizzWare.NBuilder;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Transactions;
using System.Threading.Tasks;
using System.Threading;
using FenixRepo.Core;
using FenixRepo.Context.Migrations;

namespace FenixRepo.Tests
{
    [TestClass]
    public class DatabaseTests
    {
        IFenixRepository<Person> personRepository { get; set; }
        IFenixRepository<Context.Models.Address> addressRepository { get; set; }
        static int AddressId { get; set; }
        static int CityId { get; set; }        

        public DatabaseTests()
        {            
            FenixRepositoryScriptExtractor.Initialize(() => new Context.Context(), new Configuration());
            personRepository = new FenixRepository<Person>();
            addressRepository = new FenixRepository<Context.Models.Address>();
        }

        static List<Person> CreatePeople(int number)
        {
            return Builder<Person>.CreateListOfSize(number)
                                .All()
                                    .With(c => c.FirstName = Faker.Name.First())
                                    .With(c => c.LastName = Faker.Name.Last())
                                    .With(c => c.Age = Faker.RandomNumber.Next(20, 50))
                                    .With(c => c.BirthDay = DateTime.Now.AddYears(-c.Age))
                                    .With(c => c.AddressId = AddressId)
                                .Build().ToList();
        }

        static List<Context.Models.Address> CreateAddresses(int number)
        {
            return Builder<Context.Models.Address>.CreateListOfSize(number)
                                .All()
                                    .With(c => c.CityId = CityId)
                                    .With(c => c.Street = Faker.Address.StreetName())
                                    .With(c => c.PostalCode = Faker.Address.ZipCode())                                    
                                .Build().ToList();
        }

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            using (var ctx = new Context.Context())
            {
                ctx.Database.Delete();
                ctx.Database.Create();
                ctx.Database.Initialize(false);

                var city = ctx.Cities.Add(new Context.Models.City { Name = Faker.Address.City() });
                ctx.SaveChanges();
                CityId = city.Id;

                var addr = ctx.Addresses.Add(new Context.Models.Address { City = city, Street = Faker.Address.StreetName(), PostalCode = Faker.Address.ZipCode() });
                ctx.SaveChanges();
                AddressId = addr.Id;

                ctx.People.AddRange(CreatePeople(1000));
                ctx.SaveChanges();
            }
        }        

        [TestCleanup()]
        public void Cleanup()
        {
            using (var context = new Context.Context())
            {
                context.Database.ExecuteSqlCommand("delete from dbo.People where Id > 1000");
                context.Database.ExecuteSqlCommand("delete from dbo.Addresses where Id > 1");
            }
        }

        [TestMethod]
        public void Add_People()
        {
            AddTemplate<Person, Person>("People", count => CreatePeople(count), item => personRepository.Add(item), 21000);
        }
        [TestMethod]
        public void AddRange_People()
        {
            AddTemplate<Person, List<Person>>("People", count => Enumerable.Range(0, count).Select(x => CreatePeople(2)).ToList(), item => personRepository.AddRange(item), 41000);
        }        

        [TestMethod]
        public void Add_Addresses()
        {
            AddTemplate<Context.Models.Address, Context.Models.Address>("Addresses", count => CreateAddresses(count), item => addressRepository.Add(item), 20001);
        }
        [TestMethod]
        public void AddRange_Addresses()
        {
            AddTemplate<Context.Models.Address, List<Context.Models.Address>>("Addresses", count => Enumerable.Range(0, count).Select(x => CreateAddresses(2)).ToList(), item => addressRepository.AddRange(item), 40001);
        }        

        private void AddTemplate<T, TOut>(string tableName, Func<int, List<TOut>> factory, Action<TOut> action, int assert) where T : class where TOut: class
        {            
            var tasks = new Task[10];
            var k = 0;
            for (var i = 0; i < 10; i++)
                tasks[i] = Task.Factory.StartNew((index) =>
                {
                    var items = factory(2000);
                    foreach (var item in items)
                    {
                        var j = items.IndexOf(item);
                        if ((int)index == 0 && j != 0 && j % 300 == 0)
                        {
                            k++;
                            using (var context = new Context.Context())
                            {
                                context.Database.ExecuteSqlCommand($"exec sp_rename 'dbo.{tableName}', '{tableName}{j / 300}'");
                            }
                        }
                        action(item);
                    }
                }, i);

            Task.WaitAll(tasks);

            using (var context = new Context.Context())
            {
                var queries = Enumerable.Range(0, k + 1).Select((x, i) => $"{tableName}{(i == 0 ? "" : i.ToString())}").ToList();
                Assert.AreEqual(assert, context.Database.SqlQuery<T>(queries.Select(x => $"select * from dbo.{x}").Aggregate((a, b) => $"{a}\nunion all\n{b}")).Count());

                context.Database.ExecuteSqlCommand(queries.Where(x => tableName == "People" ? (x != "People") : x != "Addresses1").Select(x => $"drop table dbo.{x}").Aggregate((a, b) => $"{a}\n{b}"));
                if (tableName == "Addresses")
                    context.Database.ExecuteSqlCommand($"exec sp_rename 'dbo.{tableName}1', '{tableName}'");
            }                          
        }
    }
}
