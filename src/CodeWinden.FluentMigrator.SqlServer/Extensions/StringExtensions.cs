using System;
using System.Data.Common;

namespace CodeWinden.FluentMigrator.SqlServer;

/// <summary>
/// String extensions
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Removes sensitive information from a connection string, such as passwords.
    /// </summary>
    /// <param name="connectionString">The connection string to sanitize.</param>
    /// <returns>A sanitized connection string without sensitive information.</returns>
    public static string RemoveConnectionStringSecrets(this string connectionString)
    {
        // Use DbConnectionStringBuilder to parse the connection string
        var connectionStringBuilder = new DbConnectionStringBuilder();

        // Set the connection string
        connectionStringBuilder.ConnectionString = connectionString;

        // Remove sensitive information
        connectionStringBuilder.Remove("password");

        // Return sanitized connection string
        return connectionStringBuilder.ToString();
    }
}
