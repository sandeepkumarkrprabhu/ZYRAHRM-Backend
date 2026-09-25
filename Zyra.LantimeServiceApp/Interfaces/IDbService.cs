using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IDbService
    {
        //Task<List<T>> ExecuteQueryAsync<T>(
        //string storedProcedure,
        //Func<SqlDataReader, T> map,
        //SqlParameter[] parameters = null);

        //Task<int> ExecuteNonQueryAsync(
        //    string storedProcedure,
        //    SqlParameter[] parameters = null);


        Task<List<T>> ExecuteQueryAsync<T>(
        string query,
        Func<SqlDataReader, T> map,
        SqlParameter[] parameters = null,
        CommandType commandType = CommandType.Text);

    }
}
