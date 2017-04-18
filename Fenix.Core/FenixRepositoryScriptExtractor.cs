using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;

namespace FenixRepo.Core
{
    public class FenixRepositoryScriptExtractor
    {
        public class FenixScript
        {
            public string TableScript { get; set; }
            public List<string> FkScripts { get; set; }
            public List<string> IndexScripts { get; set; }            

            public string getBaseScript()
            {                
                var answer = Regex.Replace(TableScript, "(?i)primary key", m => $"constraint [{Guid.NewGuid()}] {m.Value}");
                if (FkScripts.Count > 0)
                    answer += FkScripts.Select(x => Regex.Replace(x, @"(?i)(?<=add constraint\s+\[).+?\]", m => $"{Guid.NewGuid()}]")).Aggregate((a, b) => $"{a}\r\n{b}");                
                return answer;
            }
        }

        static string ExtractTableScript(string tableName, string script)
        {            
            return Regex.Match(script, $@"(?i)create table {tableName} \((.|\n)+?\);").Value;
        }
        static List<string> ExtractFkScripts(string tableName, string script)
        {
            var FKscripts = Regex.Matches(script, $@"(?i)alter table {tableName}.+?;");
            var FKscriptsList = new List<string>();
            foreach (Match fk in FKscripts)
                FKscriptsList.Add(fk.Value);
            return FKscriptsList;
        }
        static List<string> ExtractIndexScripts(string tableName, string script)
        {
            var IndexScripts = Regex.Matches(script, $@"(?i)(drop|create) index.+?on {tableName}.*?(?=\r)", RegexOptions.Multiline);
            var IndexScriptsList = new List<string>();
            foreach (Match fk in IndexScripts)
                IndexScriptsList.Add(fk.Value);
            return IndexScriptsList;
        }
        protected static string ExtractScriptFromMigration(string script, string migrationName = null)
        {
            var migrationHistory = @"\[dbo\]\.\[__MigrationHistory\]";
            return Regex.Match(script, migrationName == "0" ? $@"(?i)(?<=BEGIN\r\n)(.|\n)+(?=CREATE TABLE {migrationHistory})" : $@"(?i)(.|\n)+(?=INSERT {migrationHistory})").Value.Trim();
        }

        static string GetFullTableNameAndSchema(Type type, DbContext context)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            //Get the entity type from the model that maps to the CLR type
            var entityType = metadata.GetItems<EntityType>(DataSpace.OSpace).Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata.GetItems<EntityContainer>(DataSpace.CSpace).Single().EntitySets.Single(s => s.ElementType.Name == entityType.Name);

            //// Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace).Single().EntitySetMappings.Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            var table = mapping.EntityTypeMappings.Single().Fragments.Single().StoreEntitySet;
            
            // Return the table name from the storage entity set
            return $@"\[{(string)table.MetadataProperties["Schema"].Value ?? table.Schema}\]\.\[{(string)table.MetadataProperties["Table"].Value ?? table.Table}\]";
        }        

        protected static Dictionary<Type, FenixScript> Scripts { get; set; }
        protected static Func<DbContext> Factory { get; set; }
        protected static DbMigrationsConfiguration Configuration { get; set; }

        public static void Initialize<TDbContext>(Func<TDbContext> contextFactory, DbMigrationsConfiguration<TDbContext> contextConfiguration) where TDbContext : DbContext
        {
            Factory = contextFactory;
            Configuration = contextConfiguration;

            var tableTypes = typeof(TDbContext).GetProperties().Where(x => x.PropertyType.FullName.Contains("System.Data.Entity.DbSet")).Select(x => x.PropertyType.GenericTypeArguments.First()).ToList();
            
            var migrator = new DbMigrator(Configuration);
            var scriptor = new MigratorScriptingDecorator(migrator);
            var migrationScript = scriptor.ScriptUpdate("0", null);

            using (var context = contextFactory())
            {
                var baseScripts = ((IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript();

                Scripts = tableTypes.ToDictionary(x => x, x => {
                    var tableName = GetFullTableNameAndSchema(x, context);
                    return new FenixScript
                    {
                        TableScript = ExtractTableScript(tableName, baseScripts),
                        FkScripts = ExtractFkScripts(tableName, baseScripts),
                        IndexScripts = ExtractIndexScripts(tableName, migrationScript)
                    };
                });
            }
        }
    }
}
