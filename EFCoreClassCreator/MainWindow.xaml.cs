using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
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

        private void Button_Generate_Class_Code_Click(object sender, RoutedEventArgs e)
        {
            DataTable schemaTable;
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
            List<DataArray> DataSet = new();
            foreach (DataRow item in schemaTable.Rows)
            {
                DataSet.Add(new DataArray()
                {
                    ColName = item.ItemArray[0].ToString(),
                    ColType = item.ItemArray[11].ToString(),
                    ColLength = Convert.ToInt32(item.ItemArray[2]),
                    ColNullable = Convert.ToBoolean(item.ItemArray[12])
                });
            }

            StringBuilder Output = new();

            Output.AppendLine("using MySql.Data.MySqlClient;");
            Output.AppendLine("using System;");
            Output.AppendLine("using System.Collections.Generic;");
            Output.AppendLine();

            Output.AppendLine("namespace " + TextBox_Namespace.Text);
            Output.AppendLine("{");
            Output.AppendLine("public class " + TextBox_ClassName.Text);
            Output.AppendLine("{");
            Output.AppendLine($"public static List<data{TextBox_ClassName.Text}> DN{TextBox_ClassName.Text}()");
            Output.AppendLine("{");
            Output.AppendLine($"List<data{TextBox_ClassName.Text}> Data = new();");
            Output.AppendLine($"string ConnectionString = \"{TextBox_Login_Details.Text}\";");
            Output.AppendLine("using (MySqlConnection DBConnection = new MySqlConnection(ConnectionString))");
            Output.AppendLine("{");
            Output.AppendLine("DBConnection.Open();");
            Output.AppendLine($"MySqlCommand DataCommand = new MySqlCommand(@\"{TextBox_SQLCode.Text}\", DBConnection);");
            Output.AppendLine("//DataCommand.Parameters.AddWithValue(\"@\", \"\");");
            Output.AppendLine("MySqlDataReader myReader = DataCommand.ExecuteReader();");
            Output.AppendLine("while (myReader.Read())");
            Output.AppendLine("{");
            Output.AppendLine($"Data.Add(new data{TextBox_ClassName.Text}()");
            Output.AppendLine("{");

            List<string> Params = new();
            foreach (var item in DataSet)
            {

                //RETSLockLastUpdated = myReader["RETSLockLastUpdated"] != DBNull.Value ? myReader.GetDateTime("RETSLockLastUpdated") : null,

                switch (item.ColType)
                {
                    case "System.String":
                        Params.Add(item.ColName + " = myReader[\"" + item.ColName + "\"].ToString()");
                        break;
                    case "System.SByte":
                        if (item.ColLength == 1)
                        {
                            Params.Add(item.ColName + " = myReader[\"" + item.ColName + "\"].ToString() == \"1\"");
                        }
                        else
                        {
                            Params.Add(item.ColName + " = myReader.GetInt16(\"" + item.ColName + "\")");
                        }
                        break;
                    case "System.Int32":
                        Params.Add(item.ColName + " = myReader.GetInt32(\"" + item.ColName + "\")");
                        break;
                    case "System.DateTime":
                        Params.Add(item.ColName + " = myReader.GetDateTime(\"" + item.ColName + "\")");
                        break;
                    default:
                        Params.Add($"// No Key Match to {item.ColType} {item.ColName}");
                        break;
                }
            }

            Output.AppendLine(string.Join(",\n", Params));

            Output.AppendLine("});");
            Output.AppendLine("}");
            Output.AppendLine("myReader.Close();");
            Output.AppendLine("DBConnection.Close();");
            Output.AppendLine("}");
            Output.AppendLine("return Data;");
            Output.AppendLine("}");
            Output.AppendLine("}");

            //Handles the Class for data storage

            Dictionary<string, string> TypeConv = new();
            TypeConv.Add("System.String", "string");
            TypeConv.Add("System.SByte", "sbyte");
            TypeConv.Add("System.Int32", "int");
            TypeConv.Add("System.DateTime", "DateTime");
            //TypeConv.Add("System.", "");
            //TypeConv.Add("System.", "");

            Output.AppendLine("public class data" + TextBox_ClassName.Text);
            Output.AppendLine("{");

            foreach (var item in DataSet)
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
    }
}
