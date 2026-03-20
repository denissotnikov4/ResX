using FluentMigrator;

namespace ResX.Notifications.Infrastructure.Migrations;

[Migration(1, "Create notifications table")]
public class M001_CreateNotificationsTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS notifications;");

        Create.Table("notifications").InSchema("notifications")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(100).NotNullable()
            .WithColumn("title").AsString(200).NotNullable()
            .WithColumn("body").AsString(1000).NotNullable()
            .WithColumn("is_read").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("payload").AsCustom("jsonb").Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("read_at").AsDateTimeOffset().Nullable();

        Create.Index("ix_notifications_user_id").OnTable("notifications").InSchema("notifications")
            .OnColumn("user_id").Ascending();
        Create.Index("ix_notifications_user_unread").OnTable("notifications").InSchema("notifications")
            .OnColumn("user_id").Ascending().OnColumn("is_read").Ascending();
    }

    public override void Down()
    {
        Delete.Table("notifications").InSchema("notifications");
    }
}
