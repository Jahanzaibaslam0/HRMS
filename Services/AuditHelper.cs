using Microsoft.Data.SqlClient;

namespace HRMS.Services;

/// <summary>Helpers for CreatedByUserID / ModifiedByUserID audit columns.</summary>
public static class AuditHelper
{
    public static void AddCreatedBy(SqlCommand cmd, int? userId)
    {
        cmd.Parameters.AddWithValue("@CreatedByUserID",
            userId is > 0 ? userId.Value : DBNull.Value);
    }

    public static void AddModifiedBy(SqlCommand cmd, int? userId)
    {
        cmd.Parameters.AddWithValue("@ModifiedByUserID",
            userId is > 0 ? userId.Value : DBNull.Value);
    }
}
