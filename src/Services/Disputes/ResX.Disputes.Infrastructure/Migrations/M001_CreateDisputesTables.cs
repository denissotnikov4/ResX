using FluentMigrator;

namespace ResX.Disputes.Infrastructure.Migrations;

[Migration(1, "Create disputes tables")]
public class M001_CreateDisputesTables : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS disputes;");

        Create.Table("disputes").InSchema("disputes")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("transaction_id").AsGuid().NotNullable()
            .WithColumn("initiator_id").AsGuid().NotNullable()
            .WithColumn("respondent_id").AsGuid().NotNullable()
            .WithColumn("reason").AsString(2000).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Open")
            .WithColumn("resolution").AsString(5000).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("resolved_at").AsDateTimeOffset().Nullable();

        Create.Table("evidence").InSchema("disputes")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("dispute_id").AsGuid().NotNullable()
                .ForeignKey("fk_evidence_disputes", "disputes", "disputes", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("submitted_by").AsGuid().NotNullable()
            .WithColumn("description").AsString(2000).NotNullable()
            .WithColumn("file_urls").AsString(5000).NotNullable().WithDefaultValue("")
            .WithColumn("submitted_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);
    }

    public override void Down()
    {
        Delete.Table("evidence").InSchema("disputes");
        Delete.Table("disputes").InSchema("disputes");
    }
}
