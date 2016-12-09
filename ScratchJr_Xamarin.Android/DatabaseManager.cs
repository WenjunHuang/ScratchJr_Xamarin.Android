using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Database.Sqlite;
using Android.Util;
using Dapper;
using Mono.Data.Sqlite;

namespace ScratchJr.Android
{
    /// <summary>
    /// Manages the database connection for Scratch Jr
    /// </summary>
    /// <author>Wenjun Huang</author>
    public class DatabaseManager
    {
        public const string LogTag = nameof(DatabaseManager);
        private const string DbName = "ScratchJr";
        private const int DbVersion = 1;

        private readonly Context _applicationContext;
        private IDbConnection _database;
        private string _dbPath;

        public DatabaseManager(Context applicationContext)
        {
            _applicationContext = applicationContext;
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                DbName);
        }


        public IDbConnection GetDbConnection()
        {
            bool exist = File.Exists(_dbPath);
            IDbConnection cnt = null;
            if (!exist)
            {
                Log.Info(DatabaseManager.LogTag, "Creating database");

                // database file not exist, then create a new one and initialize tables.
                SqliteConnection.CreateFile(_dbPath);
                cnt = new SqlConnection($"Data Source={_dbPath}");
                cnt.Open();
                InitializeTables(cnt);
            }
            else
            {
                cnt = new SqlConnection($"Data Source={_dbPath}");
                cnt.Open();
            }

            return cnt;
        }


        private void InitializeTables(IDbConnection db)
        {
            var trans = db.BeginTransaction();
            try
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = _applicationContext
                    .GetString(Resource.String.sql_create_projects);
                cmd.ExecuteNonQuery();
                Log.Info(DatabaseManager.LogTag, "Created table projects");

                cmd.CommandText = _applicationContext.GetString(Resource.String.sql_create_usershapes);
                cmd.ExecuteNonQuery();
                Log.Info(DatabaseManager.LogTag, "Created table usershapes");

                cmd.CommandText = _applicationContext.GetString(Resource.String.sql_create_userbkgs);
                cmd.ExecuteNonQuery();
                Log.Info(DatabaseManager.LogTag, "Created table userbkgs");

                cmd.CommandText = _applicationContext
                    .GetString(Resource.String.sql_add_gift);
                cmd.ExecuteNonQuery();
                Log.Info(DatabaseManager.LogTag, "Created project gift field");

                trans.Commit();
            }
            catch (Exception exception)
            {
                Log.Error(DatabaseManager.LogTag, $"Can not initialze tables, error: {exception.Message}");
                trans.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string statement, object param) 
        {
            using (var cnt = GetDbConnection())
            {
                return await cnt.QueryAsync<T>(statement, param);
            }
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string statment, object param)
        {
            using (var cnt = GetDbConnection())
            {
                return await cnt.QueryFirstOrDefaultAsync<T>(statment, param);
            }
        }
    }
}