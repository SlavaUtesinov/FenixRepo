# FenixRepo (EF code first) [![Build status](https://ci.appveyor.com/api/projects/status/9valki8iixrbwr35?svg=true)](https://ci.appveyor.com/project/SlavaUtesinov/fenixrepo) [![Version](https://img.shields.io/nuget/v/FenixRepo.svg)](https://www.nuget.org/packages/FenixRepo)

At first, read this [article](https://www.codeproject.com/script/Articles/ArticleVersion.aspx?waid=235991&aid=1182830) with complete explanation. You can use this library to dynamically (re)create particular table(if it not exists), that is a part of you `Context` at case of insert events or simple manually create table for some reasons. 

First of all, you should call one time, at startup `Initialize` method, where first argument is a factory method, which returns instance of your `Context` and the second one is an instance of `Configuration` `class`. It will prepare SQL scripts for all of your tables, registered at your `Context`. At case of ASP.NET MVC it is a good decision to paste this code into Global.asax:

    FenixRepositoryScriptExtractor.Initialize(() => new Context(), new Configuration());

Then you can create table of desired type `MyTable` this simple way:

    var repo = new FenixRepositoryCreateTable<MyTable>();
    //or repo = new FenixRepository<MyTable>();
    
    repo.CreateTable();

Also, if your table spread between several migrations and they have nothing stuff corresponded to other tables, you can specify these migrations(i.e. names of classes from Migrations folder) via `FenixAttribute`, and exactly they will be used as source of SQL scripts, which will be used for table creation:

    [Fenix(nameof(Initial), nameof(MyTableFirstMigration), nameof(MyTableSecondMigration))]
    public class MyTable
    {
        //some stuff
    }

Without this attribute, library will use *default* scripts. It is always better to specify migrations, because otherwise it is not guaranteed that all indexes will be created and also into your migrations you can include some custom code, that will not be executed at case of *default* solution.

Library is compatible and tested with EF 6.1.3 at case of MS SQL.