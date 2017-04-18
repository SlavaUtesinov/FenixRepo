using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FenixRepo.Core
{
    public class FenixRepositoryCreateTable<T> : FenixRepositoryScriptExtractor where T : class
    {
        private string GetScriptFromMigrations()
        {
            var type = typeof(T);
            var fenixAttr = type.CustomAttributes.Where(x => x.AttributeType == typeof(FenixAttribute)).FirstOrDefault();
            if (fenixAttr != null)
            {
                var migrations = (fenixAttr.ConstructorArguments.First().Value as ReadOnlyCollection<CustomAttributeTypedArgument>).Select(x => x.Value.ToString()).ToArray();
                
                var migrator = new DbMigrator(Configuration);
                var allMigrations = migrator.GetLocalMigrations().ToList();
                var scriptor = new MigratorScriptingDecorator(migrator);

                string allMigrationScripts = null;
                foreach (var migration in migrations)
                {
                    var target = allMigrations.Where(x => x.Contains(migration)).First();
                    var targetIndex = allMigrations.IndexOf(target);
                    var source = targetIndex == 0 ? "0" : Regex.Match(allMigrations.Where(x => allMigrations.IndexOf(x) == (targetIndex - 1)).First(), @"(?<=\d+_).+").Value;                    
                    string script = scriptor.ScriptUpdate(source, target);
                    allMigrationScripts += $"{ExtractScriptFromMigration(script, source)}{"\r\n"}";
                }
                return allMigrationScripts.Trim();
            }
            return null;
        }

        public void CreateTable(DbContext context = null)
        {
            if (context == null)
                using (context = Factory())
                {
                    CreateTableInner(context);
                }
            else
                CreateTableInner(context);
        }

        private void CreateTableInner(DbContext context)
        {            
            var migrationScript = GetScriptFromMigrations();
            if (migrationScript != null)
                context.Database.ExecuteSqlCommand(migrationScript);            
            else
            {                
                var info = Scripts[typeof(T)];
                var toExclude = new List<string>();                
                context.Database.ExecuteSqlCommand(info.getBaseScript());
                foreach (var index in info.IndexScripts)
                {
                    try
                    {
                        context.Database.ExecuteSqlCommand(index);
                    }
                    catch (SqlException e) when ((index.ToLower().Contains("create") && e.Number == 3701) || (index.ToLower().Contains("drop") && e.Number == 1911))
                    {
                        toExclude.Add(index);
                    }
                }
                toExclude.ForEach(x => info.IndexScripts.Remove(x));
            }
        }
    }
}
