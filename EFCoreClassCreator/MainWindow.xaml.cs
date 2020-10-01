using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace EFCoreClassCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();
        public List<string> Parameters = new List<string>();

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
            foreach (var item in DataExtract)
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
            Output.AppendLine($"public static List<{TextBox_ClassName.Text}> Load()");
            Output.AppendLine("{");
            Output.AppendLine($"List<{TextBox_ClassName.Text}> Data = new List<{TextBox_ClassName.Text}>();");
            Output.AppendLine($"string ConnectionString = \"{TextBox_Login_Details.Text}\";");
            Output.AppendLine($"using ({MyPreFix}SqlConnection DBConnection = new {MyPreFix}SqlConnection(ConnectionString))");
            Output.AppendLine("{");
            Output.AppendLine("DBConnection.Open();");
            Output.AppendLine($"{MyPreFix}SqlCommand DataCommand = new {MyPreFix}SqlCommand(@\"{TextBox_SQLCode.Text}\", DBConnection);");
            Output.AppendLine("//DataCommand.Parameters.AddWithValue(\"@\", \"\");");

            //Parameter Builder Goes Here

            Output.AppendLine($"{MyPreFix}SqlDataReader DataReader = DataCommand.ExecuteReader();");
            Output.AppendLine("while (DataReader.Read())");
            Output.AppendLine("{");
            Output.AppendLine($"Data.Add(new {TextBox_ClassName.Text}()");
            Output.AppendLine("{");
            List<string> Params = new();
            foreach (var item in DataExtract)
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

        class DataArray
        {
            public string ColName { get; set; }
            public string ColType { get; set; }
            public int ColLength { get; set; }
            public bool ColNullable { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists("settings.ini"))
            {
                TextBox_Login_Details.Text = System.IO.File.ReadAllText("settings.ini");
            }
            DataGrid_Parameters.ItemsSource = Parameters;
        }
    }
}
