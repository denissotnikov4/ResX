using FluentMigrator;

namespace ResX.Listings.Infrastructure.Migrations;

[Migration(3, "Create categories table")]
public class M003_CreateCategoriesTable : Migration
{
    public override void Up()
    {
        Create.Table("categories").InSchema("listings")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("parent_category_id").AsGuid().Nullable()
                .ForeignKey("fk_categories_parent", "listings", "categories", "id")
            .WithColumn("icon_url").AsString(500).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("display_order").AsInt32().NotNullable().WithDefaultValue(0);

        // Seed some default categories
        Insert.IntoTable("categories").InSchema("listings").Row(new
        {
            id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            name = "Clothing",
            description = "Clothes and accessories",
            parent_category_id = (object)DBNull.Value,
            icon_url = (object)DBNull.Value,
            is_active = true,
            display_order = 1
        });

        Insert.IntoTable("categories").InSchema("listings").Row(new
        {
            id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
            name = "Electronics",
            description = "Electronic devices and gadgets",
            parent_category_id = (object)DBNull.Value,
            icon_url = (object)DBNull.Value,
            is_active = true,
            display_order = 2
        });

        Insert.IntoTable("categories").InSchema("listings").Row(new
        {
            id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
            name = "Furniture",
            description = "Home furniture and decor",
            parent_category_id = (object)DBNull.Value,
            icon_url = (object)DBNull.Value,
            is_active = true,
            display_order = 3
        });

        Insert.IntoTable("categories").InSchema("listings").Row(new
        {
            id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
            name = "Books",
            description = "Books, magazines and educational materials",
            parent_category_id = (object)DBNull.Value,
            icon_url = (object)DBNull.Value,
            is_active = true,
            display_order = 4
        });

        Insert.IntoTable("categories").InSchema("listings").Row(new
        {
            id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
            name = "Toys & Games",
            description = "Toys, games and children items",
            parent_category_id = (object)DBNull.Value,
            icon_url = (object)DBNull.Value,
            is_active = true,
            display_order = 5
        });
    }

    public override void Down()
    {
        Delete.Table("categories").InSchema("listings");
    }
}
