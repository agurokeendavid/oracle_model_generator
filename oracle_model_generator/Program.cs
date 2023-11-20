// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Oracle.ManagedDataAccess.Client;
Console.Write("Enter a database ip address: ");
string databaseIp = Console.ReadLine();
Console.Write("Enter a database port: ");
string databasePort = Console.ReadLine();
Console.Write("Enter a database sid/service name: ");
string databaseSid = Console.ReadLine();
Console.Write("Enter a database username: ");
string databaseUserName = Console.ReadLine();
Console.Write("Enter a database password: ");
string databasePassword = Console.ReadLine();
Console.Write("Enter a namespace: ");
string nameSpace = Console.ReadLine();
Console.Write("Enter the path that you want to paste the generated models: (e.g C:\\Users\\Default\\Documents\\Models\\) ");
string path = Console.ReadLine();

string connectionString =
    $"DATA SOURCE={databaseIp}:{databasePort}/{databaseSid};PASSWORD={databasePassword};PERSIST SECURITY INFO=True;USER ID={databaseUserName}";

using var connection = new OracleConnection(connectionString);
connection.Open();

var tableListCommand =
    new OracleCommand("SELECT OWNER, TABLE_NAME FROM ALL_TABLES WHERE OWNER = :OWNER ORDER BY TABLE_NAME ASC",
        connection);
tableListCommand.Parameters.Add("OWNER", OracleDbType.Varchar2).Value = databaseUserName;

var tableListReader = tableListCommand.ExecuteReader();
int totalCount = 0;
while (tableListReader.Read())
{
    string tableName = tableListReader["TABLE_NAME"].ToString();
    string[] splittedTableName = tableName.Split('_');

    for (int i = 0; i < splittedTableName.Length; i++)
    {
        splittedTableName[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(splittedTableName[i].ToLower());
    }

    string className = String.Join(String.Empty, splittedTableName);
    
    string classContent = GenerateClassFromTable(connection, databaseUserName, tableName, nameSpace, className);

// Save the generated class content to a .cs file
    string filePath = $@"{path}{className}.cs";
    File.WriteAllText(filePath, classContent);

    Console.WriteLine($"Class file generated: {filePath}");
    totalCount++;
}

Console.WriteLine("Total class generated: " + totalCount);

static string GenerateClassFromTable(OracleConnection connection, string databaseUserName, string tableName,
    string nameSpace, string className)
{
    // Retrieve column information for the specified table
    var columnInfoCommand = new OracleCommand(
        "SELECT COLUMN_NAME, DATA_TYPE, NULLABLE, DATA_PRECISION, DATA_SCALE FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = :TABLE_NAME AND OWNER = :OWNER",
        connection);
    columnInfoCommand.Parameters.Add("TABLE_NAME", OracleDbType.Varchar2).Value = tableName;
    columnInfoCommand.Parameters.Add("OWNER", OracleDbType.Varchar2).Value = databaseUserName;
    var columnInfoReader = columnInfoCommand.ExecuteReader();

    string classContent = "using System;\nnamespace " + nameSpace + "\n{\n";
    classContent += "\tpublic class " + className + "\n\t{\n";

    // Populate the dictionary with column nullability information
    while (columnInfoReader.Read())
    {
        string columnName = columnInfoReader["COLUMN_NAME"].ToString();
        string columnType = columnInfoReader["DATA_TYPE"].ToString();
        string dataPrecision = columnInfoReader["DATA_PRECISION"].ToString();
        string dataScale = columnInfoReader["DATA_SCALE"].ToString();
        bool isNullable = columnInfoReader["NULLABLE"].ToString() == "Y";

        // Convert Oracle data types to C# data types (you may need to expand this based on your needs)
        if (columnType == "NUMBER" && string.IsNullOrWhiteSpace(dataPrecision) && string.IsNullOrWhiteSpace(dataScale) && dataScale != "0")
        {
            columnType = isNullable ? "double?" : "double";
        } else if (columnType == "NUMBER")
        {
            columnType = isNullable ? "int?" : "int";
        } else if (columnType == "VARCHAR2" || columnType == "NVARCHAR2" || columnType == "CLOB")
        {
            columnType = "string";
        } else if (columnType.Contains("DATE") || columnType.Contains("TIMESTAMP"))
        {
            columnType = isNullable ? "DateTime?" : "DateTime";
        } else if (columnType == "BLOB")
        {
            columnType = "byte[]";
        }
        else
        {
            columnType = string.Empty;
        }
        // Generate property
        classContent += $"\t\tpublic {columnType} {columnName} {{ get; set; }}\n";
    }


    classContent += "\t}\n}\n";

    return classContent;
}