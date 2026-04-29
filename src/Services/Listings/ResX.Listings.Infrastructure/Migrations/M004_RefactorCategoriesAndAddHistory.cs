using FluentMigrator;

namespace ResX.Listings.Infrastructure.Migrations;

[Migration(4, "Add category timestamps, drop denormalized listing columns, create category_history table")]
public class M004_RefactorCategoriesAndAddHistory : Migration
{
    public override void Up()
    {
        Alter.Table("categories").InSchema("listings")
            .AddColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .AddColumn("updated_at").AsDateTimeOffset().Nullable();

        Delete.Column("category_name").FromTable("listings").InSchema("listings");
        Delete.Column("parent_category_id").FromTable("listings").InSchema("listings");

        Create.ForeignKey("fk_listings_category")
            .FromTable("listings").InSchema("listings").ForeignColumn("category_id")
            .ToTable("categories").InSchema("listings").PrimaryColumn("id");

        Create.Index("ix_listings_category_id").OnTable("listings").InSchema("listings")
            .OnColumn("category_id").Ascending();

        Create.Table("category_history").InSchema("listings")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("category_id").AsGuid().NotNullable()
            .WithColumn("changed_by_user_id").AsGuid().NotNullable()
            .WithColumn("change_type").AsString(50).NotNullable()
            .WithColumn("old_values_json").AsCustom("jsonb").Nullable()
            .WithColumn("new_values_json").AsCustom("jsonb").Nullable()
            .WithColumn("changed_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset);

        Create.Index("ix_category_history_category_id").OnTable("category_history").InSchema("listings")
            .OnColumn("category_id").Ascending();
        Create.Index("ix_category_history_changed_at").OnTable("category_history").InSchema("listings")
            .OnColumn("changed_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("category_history").InSchema("listings");

        Delete.Index("ix_listings_category_id").OnTable("listings").InSchema("listings");
        Delete.ForeignKey("fk_listings_category").OnTable("listings").InSchema("listings");

        Alter.Table("listings").InSchema("listings")
            .AddColumn("category_name").AsString(100).NotNullable().WithDefaultValue("")
            .AddColumn("parent_category_id").AsGuid().Nullable();

        Delete.Column("created_at").FromTable("categories").InSchema("listings");
        Delete.Column("updated_at").FromTable("categories").InSchema("listings");
    }
}
