using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

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
            return DoExecuteSql(cmd, true);
        }

        public static void SaveDataRow(DataRow row, string sprocname)
        {
            SqlCommand cmd = GetSqlCommand(sprocname);
            foreach(DataColumn col in row.Table.Columns)
            {
                string paramname = $"@{col.ColumnName}";
                if (cmd.Parameters.Contains(paramname))
                {
                    cmd.Parameters[paramname].Value = row[col.ColumnName];
                }
            }
            DoExecuteSql(cmd, false);

            foreach(SqlParameter p in cmd.Parameters)
            {
                if (p.Direction == ParameterDirection.InputOutput)
                {
                    string colname = p.ParameterName.Substring(1);
                    if (row.Table.Columns.Contains(colname))
                    {
                        row[colname] = p.Value;
                    }
                }
            }
        }



        private static DataTable DoExecuteSql(SqlCommand cmd, bool loadtable)
        {
            
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                Debug.Print(GetSQL(cmd));
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    CheckReturnValue(cmd);
                    if(loadtable == true)
                    {
                        dt.Load(dr);
                    }
                }
                catch(SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    throw new Exception(msg);
                }
                catch(InvalidCastException ex)
                {
                    throw new Exception(cmd.CommandText + ": " + ex.Message, ex);
                }
            }
            SetAllColumnsAllowNulls(dt);
            return dt;
        }

        private static void CheckReturnValue(SqlCommand cmd)
        {
            int returnvalue = 0;
            string msg = "";
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.ReturnValue)
                    {
                        if (p.Value != null)
                        {
                            returnvalue = (int)p.Value;
                        }

                    }
                    else if (p.ParameterName.ToLower() == "@message")
                    {
                        if (p.Value != null)
                        {
                            msg = p.Value.ToString();
                        }

                    }
                }
            }

            if(returnvalue == 1)
            {
                if(msg == "")
                {
                    msg = $"{cmd.CommandText} did not do action that was requested.";
                }
                throw new Exception(msg);
            }
        }


        public static DataTable GetDataTable(string sqlstatement) //- take a SQL statement and return a data table
        {
            return DoExecuteSql(new SqlCommand(sqlstatement), true);
        }

        public static void ExecuteSql(SqlCommand cmd)
        {
            DoExecuteSql(cmd, false);
        }


        public static void ExecuteSql(string sqlstatement)
        {
            GetDataTable(sqlstatement);
        }

        public static void SetParamValue(SqlCommand cmd, string paramname, object value)
        {
            try
            {
                cmd.Parameters[paramname].Value = value;
            }
            catch(Exception ex)
            {
                throw new Exception(cmd.CommandText + ": " + ex.Message, ex);
            }
        }


        private static string ParseConstraintMsg(string msg)
        {
            string origmsg = msg;

            if (msg.Contains("Cannot insert the value NULL into column"))
            {
                string pref = "Cannot insert the value NULL into column '";

                int startpos = msg.IndexOf(pref) + pref.Length;
                int endpos = msg.IndexOf("'", startpos);

                if(endpos > startpos)
                {
                    string columnname = msg.Substring(startpos, endpos - startpos);

                    columnname = columnname.Replace("WebUserId", "user")
                        .Replace("CuisineId", "Cuisine")
                        .Replace("RecipeName", "Recipe Name")
                        .Replace("DateDrafted", "Date Drafted")
                        .Replace("AmountCalories", "Amount Calories");

                    return columnname + " is required.";
                }
            }

            string prefix = "ck_";
            string msgend = "";

            if (msg.Contains(prefix) == false)
            {
                if (msg.Contains("u_"))
                {
                    prefix = "u_";
                    msgend = " must be unique.";
                }
                else if (msg.Contains("f_"))
                {
                    prefix = "f_";
                }
            }
            if (msg.Contains(prefix))
            {
                msg = msg.Replace("\"", "'");
                int pos = msg.IndexOf(prefix) + prefix.Length;
                msg = msg.Substring(pos);
                pos = msg.IndexOf("'");
                if (pos == -1)
                {
                    msg = origmsg;
                }
                else
                {
                    msg = msg.Substring(0, pos);
                    msg = msg.Replace("_", " ");
                    msg = msg + msgend;

                    if (prefix == "f_")
                    {
                        var words = msg.Split(" ");
                        if (words.Length > 1)
                        {
                            msg = $"Cannot delete {words[0]} because it has a related {words[1]} record.";
                        }
                    }
                }

            }
            return msg;
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

        public static string GetSQL(SqlCommand cmd)
        {
            string val = "";
#if DEBUG
            StringBuilder sb = new();

            if (cmd.Connection != null)
            {
                sb.AppendLine($"--{cmd.Connection.DataSource}");
                sb.AppendLine($"use {cmd.Connection.Database}");
                sb.AppendLine("go");
            }

            if(cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendLine($"exec {cmd.CommandText}");
                int paramcount = cmd.Parameters.Count - 1;
                int paramnum = 0;
                string comma = ",";
                foreach(SqlParameter p in cmd.Parameters)
                {
                    if(p.Direction != ParameterDirection.ReturnValue)
                    {
                        if (paramnum == paramcount)
                        {
                            comma = "";
                        }
                        sb.AppendLine($"{p.ParameterName} = {(p.Value == null ? "null" : p.Value.ToString())}{comma}");
                       
                    }
                    paramnum++;
                }
            }
            else
            {
                sb.AppendLine(cmd.CommandText);
            }

            val = sb.ToString();
#endif
            return val;
        }

        public static string SqlValue(object value)
        {
            if (value == DBNull.Value || value == null)
            {
                return "null";
            }
            if (value is string || value is DateTime)
            {
                return $"'{value.ToString().Replace("'", "''")}'";
            }
            return value.ToString();
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
