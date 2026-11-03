using System.Data.Common;

namespace VertexLearn.Data
{
    /// <summary>Short, null-safe helpers for reading columns by name.</summary>
    public static class DataReaderExtensions
    {
        public static int Int(this DbDataReader r, string column) => Convert.ToInt32(r[column]);

        public static int IntOrZero(this DbDataReader r, string column) =>
            r[column] == DBNull.Value ? 0 : Convert.ToInt32(r[column]);

        public static double DoubleOrZero(this DbDataReader r, string column) =>
            r[column] == DBNull.Value ? 0 : Convert.ToDouble(r[column]);

        public static string Str(this DbDataReader r, string column) =>
            r[column] == DBNull.Value ? string.Empty : Convert.ToString(r[column]) ?? string.Empty;

        public static string? StrOrNull(this DbDataReader r, string column) =>
            r[column] == DBNull.Value ? null : Convert.ToString(r[column]);

        public static DateTime Date(this DbDataReader r, string column) => Convert.ToDateTime(r[column]);

        public static DateTime? DateOrNull(this DbDataReader r, string column) =>
            r[column] == DBNull.Value ? null : Convert.ToDateTime(r[column]);

        public static bool Bool(this DbDataReader r, string column) =>
            r[column] != DBNull.Value && Convert.ToBoolean(r[column]);
    }
}
