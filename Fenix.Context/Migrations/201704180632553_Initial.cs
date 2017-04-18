namespace FenixRepo.Context.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.People",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 64),
                        Address = c.String(maxLength: 64),
                        Age = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id, name: Guid.NewGuid().ToString())
                .Index(t => t.Name)
                .Index(t => t.Address);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.People", new[] { "Address" });
            DropIndex("dbo.People", new[] { "Name" });
            DropTable("dbo.People");
        }
    }
}
