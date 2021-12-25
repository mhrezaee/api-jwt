using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebApi.Migrations
{
    public partial class AddRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"INSERT INTO AspNetRoles (Id, Name,NormalizedName)
	                                VALUES
	                                    (NEWID(), 'Admin', 'ADMIN'),
	                                    (NEWID(), 'User', 'USER')
                                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                                    DELETE FROM AspNetRoles
	                                    WHERE Name IN ('Admin','User')
                                ");
        }
    }
}
