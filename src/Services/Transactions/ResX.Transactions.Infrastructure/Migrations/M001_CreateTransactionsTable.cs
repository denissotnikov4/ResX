using FluentMigrator;

namespace ResX.Transactions.Infrastructure.Migrations;

[Migration(1, "Create transactions table")]
public class M001_CreateTransactionsTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS transactions;");

        Create.Table("transactions").InSchema("transactions")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("listing_id").AsGuid().NotNullable()
            .WithColumn("donor_id").AsGuid().NotNullable()
            .WithColumn("recipient_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Pending")
            .WithColumn("notes").AsString(1000).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().Nullable()
            .WithColumn("completed_at").AsDateTimeOffset().Nullable();

        Create.Index("ix_transactions_donor_id").OnTable("transactions").InSchema("transactions").OnColumn("donor_id");
        Create.Index("ix_transactions_recipient_id").OnTable("transactions").InSchema("transactions").OnColumn("recipient_id");
        Create.Index("ix_transactions_listing_id").OnTable("transactions").InSchema("transactions").OnColumn("listing_id");
    }

    public override void Down()
    {
        Delete.Table("transactions").InSchema("transactions");
    }
}
