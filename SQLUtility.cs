using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Xml.Serialization;

namespace CPUFramework
{
    public class SQLUtility
    {
        public static string ConnectionString = "";

        public static SqlCommand GetSqlCommand(String sprocname)
        {
            SqlCommand cmd;
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                cmd = new SqlCommand(sprocname, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);
            }
            return cmd;
        }

        public static DataTable GetDataTable(SqlCommand cmd)
        {
            Debug.Print("-----" + Environment.NewLine + cmd.CommandText);
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
            }
            SetAllColumnsAllowNulls(dt);
            return dt;
        }


        public static DataTable GetDataTable(string sqlstatement) //- take a SQL statement and return a data table
        {
            return GetDataTable(new SqlCommand(sqlstatement));
        }

        public static void ExecuteSql(string sqlstatement)
        {
            GetDataTable(sqlstatement);
        }

        public static int GetFirstColumnFirstRowValue(string sql)
        {
            int n = 0;

            DataTable dt = GetDataTable(sql);
            if(dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if(dt.Rows[0][0] != DBNull.Value)
                {
                    
                    int.TryParse(dt.Rows[0][0].ToString(), out n);
                }
                
            }
            return n;
        }   

        public static string GetFirstColumnFirstRowString(string sql)
        {
            DataTable dt = GetDataTable(sql);

            if(dt.Rows.Count == 0)
            {
                return "";
            }

            return dt.Rows[0][0].ToString();

        }

        public static DateTime GetFirstColumnFirstRowsDateTime(string sql)
        {
            DataTable dt = GetDataTable(sql);

            return Convert.ToDateTime(dt.Rows[0][0]);
        }


        private static void SetAllColumnsAllowNulls(DataTable dt)
        {
            foreach(DataColumn c in dt.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach(DataRow r in dt.Rows)
            {
                foreach(DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " = " +  r[c.ColumnName].ToString());
                }
            }
        }
    }
}
