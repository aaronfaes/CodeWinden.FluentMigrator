using System;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.VersionTableInfo;

namespace CodeWinden.FluentMigrator.SqlServer;

/// <summary>
/// Custom version table metadata for FluentMigrator
/// </summary>
public class MigratorVersionTableMetaData : IVersionTableMetaData
{
    /// <summary>
    /// Indicates whether the version table owns its schema
    /// </summary>
    public bool OwnsSchema => true;
    /// <summary>
    /// The schema name where the version table is located
    /// </summary>
    public string SchemaName => "Migrations";
    /// <summary>
    /// The name of the version table
    /// </summary>
    public string TableName => "VersionInfo";
    /// <summary>
    /// The name of the column that stores the version number
    /// </summary>
    public string ColumnName => "Version";
    /// <summary>
    /// The name of the unique index on the version column
    /// </summary>
    public string UniqueIndexName => "UC_Version";
    /// <summary>
    /// The name of the column that stores the applied on date
    /// </summary>
    public string AppliedOnColumnName => "AppliedOn";
    /// <summary>
    /// The name of the column that stores the description
    /// </summary>
    public string DescriptionColumnName => "Description";
    /// <summary>
    /// Indicates whether to create the version table with a primary key
    /// </summary>
    public bool CreateWithPrimaryKey => true;
}
