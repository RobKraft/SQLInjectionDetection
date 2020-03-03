using System;
#if NETCOREAPP
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

class Program
{
	private static string _connectionString = "Data Source=(local);Initial Catalog=Northwind;Integrated Security=true";
	static void Main()
	{
		string userEnteredValue = "O'Malley";
		PreferredUsageExample(userEnteredValue);
		//What if you have code up the call stack that is dynamically building SQL?
		DynamicSQLInCodeExampleUsage("ProductName like " + userEnteredValue.FormatStringForSQL());
	}

#region An Example of the preferred way to format SQL - using a parameter - to prevent SQL Injection
	//https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-code-examples
	private static void PreferredUsageExample(string searchValueProvidedByUser)
	{
		string queryString = "SELECT ProductID, UnitPrice, ProductName from dbo.products "
				+ "WHERE ProductName like @ProductName";

		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			SqlCommand command = new SqlCommand(queryString, connection);
			//This is the ideal approach - to use "Parameters" to handle the SQL Injection Risk
			command.Parameters.AddWithValue("@ProductName", searchValueProvidedByUser);
			try
			{
				connection.Open();
				SqlDataReader reader = command.ExecuteReader();
#region Not Relevant To the Topics Discussed in this Article/Repo
				while (reader.Read())
				{
					Console.WriteLine("\t{0}\t{1}\t{2}", reader[0], reader[1], reader[2]);
				}
				reader.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			Console.ReadLine();
#endregion
		}
	}
#endregion
	private static void DynamicSQLInCodeExampleUsage(string searchValueProvidedByUser)
	{
		//Here we use a custom Extension method to add the single quotes.  This is superior
		//to directly adding single quotes because it allows you to change the way you format strings
		//for SQL if a time ever comes where that is needed.  It also allows you to scan the code
		//for '" or "' or '{ or '} to identify places that are not following your guideline to use your FormatSQL method
		string queryString = "SELECT ProductID, UnitPrice, ProductName from dbo.products "
				+ "WHERE ProductName like " + searchValueProvidedByUser.FormatStringForSQL();

		using (SqlConnection connection = new SqlConnection(_connectionString))
		{
			SqlCommand command = new SqlCommand(queryString, connection);
			try
			{
				connection.Open();
				SqlDataReader reader = command.ExecuteSafeReader(); //Call our Decorator-Proxy method
				if (reader != null) //If the SQL has a SQL Injection risk, I have the ExecuteSafeReader return a null
#region Not Relevant To the Topics Discussed in this Article/Repo
				{
					while (reader.Read())
					{
						Console.WriteLine("\t{0}\t{1}\t{2}", reader[0], reader[1], reader[2]);
					}
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			Console.ReadLine();
#endregion
		}
	}
}
