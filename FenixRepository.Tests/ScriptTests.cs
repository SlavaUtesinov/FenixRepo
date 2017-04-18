using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FenixRepo.Context.Models;
using System.Linq;
using FenixRepo.Core;
using System.Text.RegularExpressions;
using static FenixRepo.Core.FenixRepositoryScriptExtractor;
using FenixRepo.Context.Migrations;

namespace FenixRepo.Tests
{    
    [TestClass]
    public class ScriptTests
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            Initialize(() => new Context.Context(), new Configuration());
        }

        private void NormalizedAssertAreEqual(string expected, string actual)
        {
            Assert.AreEqual(Normalize(expected), Normalize(actual));
        }
        private string Normalize(string input)
        {
            return Regex.Replace(input, @"\s", "").Trim();
        }


        [TestMethod]
        public void MigrationScriptFromAttribute()
        {            
            var repo = new PrivateObject(new FenixRepositoryCreateTable<Person>());
            var script = (string)repo.Invoke("GetScriptFromMigrations");            

            var expected =
@"CREATE TABLE [dbo].[People] (
        [Id] [int] NOT NULL IDENTITY,
        [Name] [nvarchar](64),
        [Address] [nvarchar](64),
        [Age] [int] NOT NULL,
        CONSTRAINT [@@] PRIMARY KEY ([Id])
    )
    CREATE INDEX [IX_Name] ON [dbo].[People]([Name])
    CREATE INDEX [IX_Address] ON [dbo].[People]([Address])
IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Name' AND object_id = object_id(N'[dbo].[People]', N'U'))
    DROP INDEX [IX_Name] ON [dbo].[People]
IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Address' AND object_id = object_id(N'[dbo].[People]', N'U'))
    DROP INDEX [IX_Address] ON [dbo].[People]
ALTER TABLE [dbo].[People] ADD [FirstName] [nvarchar](128)
ALTER TABLE [dbo].[People] ADD [LastName] [nvarchar](128)
ALTER TABLE [dbo].[People] ADD [AddressId] [int] NOT NULL DEFAULT 0
ALTER TABLE [dbo].[People] ADD [BirthDay] [datetime] NOT NULL DEFAULT '1900-01-01T00:00:00.000'
CREATE INDEX [IX_Names] ON [dbo].[People]([FirstName], [LastName])
CREATE INDEX [IX_AddressId] ON [dbo].[People]([AddressId])
CREATE INDEX [IX_BirthDay] ON [dbo].[People]([BirthDay])
ALTER TABLE [dbo].[People] ADD CONSTRAINT [@@] FOREIGN KEY ([AddressId]) REFERENCES [dbo].[Addresses] ([Id]) ON DELETE CASCADE
DECLARE @var0 nvarchar(128)
SELECT @var0 = name
FROM sys.default_constraints
WHERE parent_object_id = object_id(N'dbo.People')
AND col_name(parent_object_id, parent_column_id) = 'Name';
IF @var0 IS NOT NULL
    EXECUTE('ALTER TABLE [dbo].[People] DROP CONSTRAINT [' + @var0 + ']')
ALTER TABLE [dbo].[People] DROP COLUMN [Name]
DECLARE @var1 nvarchar(128)
SELECT @var1 = name
FROM sys.default_constraints
WHERE parent_object_id = object_id(N'dbo.People')
AND col_name(parent_object_id, parent_column_id) = 'Address';
IF @var1 IS NOT NULL
    EXECUTE('ALTER TABLE [dbo].[People] DROP CONSTRAINT [' + @var1 + ']')
ALTER TABLE [dbo].[People] DROP COLUMN [Address]";

            var pattern = Regex.Replace(expected, @"[\[\]\(\)\.\+]", @"\$0");
            pattern = Regex.Replace(pattern, "@@", @".+?");            
            Assert.IsTrue(Regex.IsMatch(Normalize(script), Normalize(pattern)));
        }

        [TestMethod]
        public void BaseMigrationScript()
        {
            var repo = new PrivateType(typeof(FenixRepositoryScriptExtractor));            
            var scripts = (Dictionary<Type, FenixScript>)repo.GetStaticProperty("Scripts");
            var personScripts = scripts[typeof(Person)];

            var expected =
@"create table [dbo].[People] (
    [Id] [int] not null identity,
    [FirstName] [nvarchar](128) null,
    [LastName] [nvarchar](128) null,                
    [Age] [int] not null,                 
    [AddressId] [int] not null,              
    [BirthDay] [datetime] not null,
    primary key ([Id])                           
);";

            NormalizedAssertAreEqual(expected, personScripts.TableScript);
            NormalizedAssertAreEqual("alter table [dbo].[People] add constraint [Person_Address] foreign key ([AddressId]) references [dbo].[Addresses]([Id]) on delete cascade;", personScripts.FkScripts.First());

            var expectedIndexes =
@"CREATE INDEX [IX_Name] ON [dbo].[People]([Name])
CREATE INDEX [IX_Address] ON [dbo].[People]([Address])
DROP INDEX [IX_Name] ON [dbo].[People]
DROP INDEX [IX_Address] ON [dbo].[People]
CREATE INDEX [IX_Names] ON [dbo].[People]([FirstName], [LastName])
CREATE INDEX [IX_AddressId] ON [dbo].[People]([AddressId])
CREATE INDEX [IX_BirthDay] ON [dbo].[People]([BirthDay])";
            NormalizedAssertAreEqual(expectedIndexes, personScripts.IndexScripts.Aggregate((a, b) => $"{a}\r\n{b}"));

            expected =
@"create table [dbo].[Addresses] (
    [Id] [int] not null identity,
    [PostalCode] [nvarchar](64) null,
    [Street] [nvarchar](64) null,
    [CityId] [int] not null,
    primary key ([Id])
);";
            var addrScripts = scripts[typeof(Context.Models.Address)];
            NormalizedAssertAreEqual(expected, addrScripts.TableScript);

            expectedIndexes =
@"CREATE INDEX [IX_PostalCode] ON [dbo].[Addresses]([PostalCode])
CREATE INDEX [IX_CityId] ON [dbo].[Addresses]([CityId])";
            NormalizedAssertAreEqual(expectedIndexes, addrScripts.IndexScripts.Aggregate((a, b) => $"{a}\r\n{b}"));
        }
    }
}
