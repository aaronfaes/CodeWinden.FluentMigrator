using FluentMigrator.Runner.VersionTableInfo;

namespace CodeWinden.FluentMigrator.Tests;

public partial class MigratorOptionsBuilderTests
{
    private class TestVersionTableMetaData : IVersionTableMetaData
    {
        public object ApplicationContext { get; set; } = null!;
        public bool OwnsSchema => true;
        public string SchemaName => "dbo";
        public string TableName => "TestVersionInfo";
        public string ColumnName => "Version";
        public string UniqueIndexName => "UC_Version";
        public string AppliedOnColumnName => "AppliedOn";
        public string DescriptionColumnName => "Description";
        public bool CreateWithPrimaryKey => true;
    }
}
