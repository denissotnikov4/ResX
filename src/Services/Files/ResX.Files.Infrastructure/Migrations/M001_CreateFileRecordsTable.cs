using FluentMigrator;

namespace ResX.Files.Infrastructure.Migrations;

[Migration(1, "Create file records table")]
public class M001_CreateFileRecordsTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS files;");

        Create.Table("file_records").InSchema("files")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("original_name").AsString(500).NotNullable()
            .WithColumn("storage_key").AsString(1000).NotNullable()
            .WithColumn("url").AsString(2000).NotNullable()
            .WithColumn("content_type").AsString(200).NotNullable()
            .WithColumn("size_bytes").AsInt64().NotNullable()
            .WithColumn("uploaded_by").AsGuid().NotNullable()
            .WithColumn("is_deleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset);

        Create.Index("ix_file_records_uploaded_by").OnTable("file_records").InSchema("files")
            .OnColumn("uploaded_by").Ascending();
    }

    public override void Down()
    {
        Delete.Table("file_records").InSchema("files");
    }
}
