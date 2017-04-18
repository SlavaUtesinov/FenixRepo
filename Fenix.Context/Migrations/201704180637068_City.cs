namespace FenixRepo.Context.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class City : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Addresses", "CityId", c => c.Int(nullable: false));
            CreateIndex("dbo.Addresses", "PostalCode");
            CreateIndex("dbo.Addresses", "CityId");
            AddForeignKey("dbo.Addresses", "CityId", "dbo.Cities", "Id", cascadeDelete: true);
            DropColumn("dbo.Addresses", "City");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Addresses", "City", c => c.String(maxLength: 64));
            DropForeignKey("dbo.Addresses", "CityId", "dbo.Cities");
            DropIndex("dbo.Addresses", new[] { "CityId" });
            DropIndex("dbo.Addresses", new[] { "PostalCode" });
            DropColumn("dbo.Addresses", "CityId");
            DropTable("dbo.Cities");
        }
    }
}
