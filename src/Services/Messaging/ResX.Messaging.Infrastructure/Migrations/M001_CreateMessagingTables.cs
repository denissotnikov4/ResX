using FluentMigrator;

namespace ResX.Messaging.Infrastructure.Migrations;

[Migration(1, "Create messaging tables")]
public class M001_CreateMessagingTables : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS messaging;");

        Create.Table("conversations").InSchema("messaging")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("participants").AsString(500).NotNullable()
            .WithColumn("listing_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("last_message_at").AsDateTimeOffset().Nullable();

        Create.Table("messages").InSchema("messaging")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("conversation_id").AsGuid().NotNullable()
                .ForeignKey("fk_messages_conversations", "messaging", "conversations", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("sender_id").AsGuid().NotNullable()
            .WithColumn("content").AsString(2000).NotNullable()
            .WithColumn("sent_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("is_read").AsBoolean().NotNullable().WithDefaultValue(false);

        Create.Index("ix_messages_conversation_id").OnTable("messages").InSchema("messaging")
            .OnColumn("conversation_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("messages").InSchema("messaging");
        Delete.Table("conversations").InSchema("messaging");
    }
}
