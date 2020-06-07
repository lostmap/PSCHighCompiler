using System.Data.SqlClient;
using System.IO;
using System.Data;
using System.Data.SqlTypes;
using System;
using System.Text;

namespace ConnectSQLServer
{
    class Program
    {
        private static string connectionString = @"Data Source=OLEG-PC94\SQLEXPRESS;Initial Catalog=abalyzer;Integrated Security=True";

        static void conn_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            Console.Write(e.Message);
        }

        public static void Func_call()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();      // start connection

                string codetest = "NARG 0 PUSH 2 MULT HALT";    // source code

                //*****************************************************************//
                // CompileUDF @code nvarchar(1024), @byteCode binary(1024) OUTPUT  //
                //                                                                 //
                //*****************************************************************//
                SqlCommand compile = new SqlCommand("CompileUDF", connection);
                compile.CommandType = CommandType.StoredProcedure;


                SqlParameter code = new SqlParameter        // pass source code
                {
                    ParameterName = "@code",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 1024,
                    Direction = ParameterDirection.Input,
                    Value = codetest
                };
                
                byte[] bcode = new byte[1024];

                SqlParameter byCode = new SqlParameter      // return bytecode
                {
                    ParameterName = "@byteCode",
                    SqlDbType = SqlDbType.Binary,
                    Direction = ParameterDirection.InputOutput,
                    Value = bcode
                };
                
                compile.Parameters.Add(code);
                compile.Parameters.Add(byCode);

                compile.ExecuteNonQuery();

                bcode = (byte[])(compile.Parameters["@byteCode"].Value);


                   
                //*****************************************************************************//
                // NumRun1UDF @byteCode binary(1024), @output float OUTPUT, @arg0 binary(100)  //
                //                                                                             //
                //*****************************************************************************//
                SqlCommand run = new SqlCommand("NumRun1UDF", connection);
                run.CommandType = CommandType.StoredProcedure;

                SqlParameter byteCode = new SqlParameter    // pass bytecode from compiler
                {
                    ParameterName = "@byteCode",
                    SqlDbType = SqlDbType.Binary,
                    Direction = ParameterDirection.Input,
                    Value = bcode
                };
                
                SqlParameter output = new SqlParameter      // return float
                {
                    ParameterName = "@output",
                    Direction = ParameterDirection.Output,
                    SqlDbType = SqlDbType.Float,
                };
                
                double num = 3.6;
                byte[] barg = BitConverter.GetBytes(num); 

                SqlParameter arg = new SqlParameter         // pass numeric argument
                {
                    ParameterName = "@arg0",
                    Value = barg,
                    SqlDbType = SqlDbType.Binary,
                    Direction = ParameterDirection.Input
                };
    
                run.Parameters.Add(byteCode);
                run.Parameters.Add(output);
                run.Parameters.Add(arg);
               
                run.ExecuteNonQuery();

                double contractID = (double) run.Parameters["@output"].Value;
                Console.WriteLine(contractID);              // print result it console 

                connection.Close();
            }
        }

        static void Main(string[] args)
        {
            Func_call();
        }
    }
}
