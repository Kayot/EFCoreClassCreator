using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;

namespace EFCoreClassCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();
        public List<ParametersListItems> Parameters = new();

        private void Button_Generate_Class_Code_Click(object sender, RoutedEventArgs e)
        {
            DataTable schemaTable;
            List<DataArray> DataExtract = new();
            string MyPreFix = "";
            if (RadioButton_MySQL.IsChecked == true)
            {
                MyPreFix = "My";
                using (MySqlConnection DBConnection = new MySqlConnection(TextBox_Login_Details.Text))
                {
                    using (MySqlCommand DBCommand = new MySqlCommand(TextBox_SQLCode.Text, DBConnection))
                    {
                        foreach (ParametersListItems item in Parameters)
                        {
                            if (item.ColName.Length > 0)
                            {
                                DBCommand.Parameters.AddWithValue("@" + item.ColName, item.ColTest);
                            }
                        }
                        DBConnection.Open();
                        MySqlDataReader DBReader;
                        DBReader = DBCommand.ExecuteReader(CommandBehavior.KeyInfo);
                        schemaTable = DBReader.GetSchemaTable();
                        DBConnection.Close();
                    }
                }
                foreach (DataRow item in schemaTable.Rows)
                {
                    DataExtract.Add(new DataArray()
                    {
                        ColName = item.ItemArray[0].ToString(),
                        ColType = item.ItemArray[11].ToString(),
                        ColLength = Convert.ToInt32(item.ItemArray[2]),
                        ColNullable = Convert.ToBoolean(item.ItemArray[12])
                    });
                }
            }
            else
            {
                using (SqlConnection DBConnection = new SqlConnection(TextBox_Login_Details.Text))
                {
                    using (SqlCommand DBCommand = new SqlCommand(TextBox_SQLCode.Text, DBConnection))
                    {
                        foreach (ParametersListItems item in Parameters)
                        {
                            if (item.ColName.Length > 0)
                            {
                                DBCommand.Parameters.AddWithValue("@" + item.ColName, item.ColTest);
                            }
                        }
                        DBConnection.Open();
                        SqlDataReader DBReader;
                        DBReader = DBCommand.ExecuteReader(CommandBehavior.KeyInfo);
                        schemaTable = DBReader.GetSchemaTable();
                        DBConnection.Close();
                    }
                }
                foreach (DataRow item in schemaTable.Rows)
                {
                    DataExtract.Add(new DataArray()
                    {
                        ColName = item.ItemArray[0].ToString(),
                        ColType = item.ItemArray[12].ToString(),
                        ColLength = Convert.ToInt32(item.ItemArray[2]),
                        ColNullable = Convert.ToBoolean(item.ItemArray[13])
                    });
                }
            }
            StringBuilder Output = new();
            if (RadioButton_MySQL.IsChecked == true)
            {
                Output.AppendLine($"using MySql.Data.MySqlClient;");
            }
            else
            {
                Output.AppendLine($"using System.Data.SqlClient;");
            }
            Output.AppendLine("using System;");
            Output.AppendLine("using System.Collections.Generic;");
            Output.AppendLine();
            Output.AppendLine("namespace " + TextBox_Namespace.Text);
            Output.AppendLine("{");
            Output.AppendLine("public class " + TextBox_ClassName.Text);
            Output.AppendLine("{");
            Dictionary<string, string> TypeConv = new();
            TypeConv.Add("System.String", "string");
            TypeConv.Add("System.SByte", "sbyte");
            TypeConv.Add("System.Int32", "int");
            TypeConv.Add("System.DateTime", "DateTime");
            foreach (DataArray item in DataExtract)
            {
                string NullMark = "";
                if (item.ColNullable && item.ColType != "System.String") { NullMark = "?"; }
                if (TypeConv.ContainsKey(item.ColType))
                {
                    if (item.ColType == "System.SByte" && item.ColLength == 1)
                    {
                        Output.AppendLine($"\tpublic bool{NullMark} " + item.ColName + " { get; set; } // Length: " + item.ColLength);
                    }
                    else
                    {
                        Output.AppendLine("\tpublic " + TypeConv[item.ColType] + $"{NullMark} " + item.ColName + " { get; set; } // Length: " + item.ColLength);
                    }
                }
                else
                {
                    Output.AppendLine($"// No Key Match to {NullMark} {item.ColType} {item.ColName}");
                }
            }
            Output.AppendLine();
            List<string> LoadParam = new();
            foreach (ParametersListItems item in Parameters)
            {
                if (item.ColName.Length > 0)
                {
                    LoadParam.Add($"{item.ColType} {item.ColName}");
                }
            }
            Output.AppendLine($"public static List<{TextBox_ClassName.Text}> Load({string.Join(",", LoadParam)})");
            Output.AppendLine("{");
            Output.AppendLine($"List<{TextBox_ClassName.Text}> Data = new List<{TextBox_ClassName.Text}>();");
            Output.AppendLine($"string ConnectionString = \"{TextBox_Login_Details.Text}\";");
            Output.AppendLine($"using ({MyPreFix}SqlConnection DBConnection = new {MyPreFix}SqlConnection(ConnectionString))");
            Output.AppendLine("{");
            Output.AppendLine("DBConnection.Open();");
            Output.AppendLine($"{MyPreFix}SqlCommand DataCommand = new {MyPreFix}SqlCommand(@\"{TextBox_SQLCode.Text}\", DBConnection);");
            foreach (ParametersListItems item in Parameters)
            {
                if (item.ColName.Length > 0)
                {
                    Output.AppendLine($"DataCommand.Parameters.AddWithValue(\"@{item.ColName}\", \"{item.ColName}\");");
                }
            }
            Output.AppendLine($"{MyPreFix}SqlDataReader DataReader = DataCommand.ExecuteReader();");
            Output.AppendLine("while (DataReader.Read())");
            Output.AppendLine("{");
            Output.AppendLine($"Data.Add(new {TextBox_ClassName.Text}()");
            Output.AppendLine("{");
            List<string> Params = new();
            foreach (DataArray item in DataExtract)
            {

                //RETSLockLastUpdated = DataReader["RETSLockLastUpdated"] != DBNull.Value ? DataReader.GetDateTime("RETSLockLastUpdated") : null,

                switch (item.ColType)
                {
                    case "System.String":
                        Params.Add(item.ColName + " = DataReader[\"" + item.ColName + "\"].ToString()");
                        break;
                    case "System.SByte":
                        if (item.ColLength == 1)
                        {
                            Params.Add(item.ColName + " = DataReader[\"" + item.ColName + "\"].ToString() == \"1\"");
                        }
                        else
                        {
                            Params.Add(item.ColName + " = DataReader.GetInt16(\"" + item.ColName + "\")");
                        }
                        break;
                    case "System.Int32":
                        Params.Add(item.ColName + " = DataReader.GetInt32(\"" + item.ColName + "\")");
                        break;
                    case "System.DateTime":
                        //Params.Add(item.ColName + " = myReader.GetDateTime(\"" + item.ColName + "\")");
                        //DataReader.GetOrdinal("column")

                        Params.Add(item.ColName + " = DataReader[\"" + item.ColName + "\"] != DBNull.Value ? DataReader.GetDateTime(DataReader.GetOrdinal(\"" + item.ColName + "\")) : (DateTime?)null");

                        break;
                    default:
                        Params.Add($"// No Key Match to {item.ColType} {item.ColName}");
                        break;
                }
            }
            Output.AppendLine(string.Join(",\n", Params));
            Output.AppendLine("});");
            Output.AppendLine("}");
            Output.AppendLine("DataReader.Close();");
            Output.AppendLine("DBConnection.Close();");
            Output.AppendLine("}");
            Output.AppendLine("return Data;");
            Output.AppendLine("}");
            Output.AppendLine("}");
            Output.AppendLine("}");
            TextBox_ClassCode.Text = Output.ToString();
        }

        public class ParametersListItems
        {
            public string ColName { get; set; }
            public string ColTest { get; set; }
            public string ColType { get; set; }
        }

        public class DataArray
        {
            public string ColName { get; set; }
            public string ColType { get; set; }
            public int ColLength { get; set; }
            public bool ColNullable { get; set; }
        }

        public enum OrderStatus { None, New, Processing, Shipped, Received };

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists("settings.ini"))
            {
                string[] Lines = System.IO.File.ReadAllLines("settings.ini");
                foreach (string Line in Lines)
                {
                    if (Line.Contains('='))
                    {
                        string[] split = Line.Split('=');
                        if (split[0] == "TextBox_Login_Details") { TextBox_Login_Details.Text = Line.Replace("TextBox_Login_Details=", ""); }
                        if (split[0] == "TextBox_Namespace") { TextBox_Namespace.Text = split[1]; }
                        if (split[0] == "TextBox_ClassName") { TextBox_ClassName.Text = split[1]; }
                        if (split[0] == "DBType" && split[1] == "0") { RadioButton_MySQL.IsChecked = true; }
                        if (split[0] == "DBType" && split[1] == "1") { RadioButton_MSSQL.IsChecked = true; }
                    }
                }
            }
            DataGrid_Parameters.ItemsSource = Parameters;
        }

        private void Button_Clear_List_Click(object sender, RoutedEventArgs e)
        {
            Parameters.Clear();
            DataGrid_Parameters.ItemsSource = null;
            DataGrid_Parameters.ItemsSource = Parameters;
        }
    }
}
