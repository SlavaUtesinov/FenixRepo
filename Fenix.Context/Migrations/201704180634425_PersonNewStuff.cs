namespace FenixRepo.Context.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PersonNewStuff : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.People", new[] { "Name" });
            DropIndex("dbo.People", new[] { "Address" });
            AddColumn("dbo.People", "FirstName", c => c.String(maxLength: 128));
            AddColumn("dbo.People", "LastName", c => c.String(maxLength: 128));
            AddColumn("dbo.People", "AddressId", c => c.Int(nullable: false));
            AddColumn("dbo.People", "BirthDay", c => c.DateTime(nullable: false));
            CreateIndex("dbo.People", new[] { "FirstName", "LastName" }, name: "IX_Names");
            CreateIndex("dbo.People", "AddressId");
            CreateIndex("dbo.People", "BirthDay");
            AddForeignKey("dbo.People", "AddressId", "dbo.Addresses", "Id", cascadeDelete: true, name: Guid.NewGuid().ToString());
            DropColumn("dbo.People", "Name");
            DropColumn("dbo.People", "Address");
        }
        
        public override void Down()
        {
            AddColumn("dbo.People", "Address", c => c.String(maxLength: 64));
            AddColumn("dbo.People", "Name", c => c.String(maxLength: 64));
            DropForeignKey("dbo.People", "AddressId", "dbo.Addresses");
            DropIndex("dbo.People", new[] { "BirthDay" });
            DropIndex("dbo.People", new[] { "AddressId" });
            DropIndex("dbo.People", "IX_Names");
            DropColumn("dbo.People", "BirthDay");
            DropColumn("dbo.People", "AddressId");
            DropColumn("dbo.People", "LastName");
            DropColumn("dbo.People", "FirstName");
            CreateIndex("dbo.People", "Address");
            CreateIndex("dbo.People", "Name");
        }
    }
}
