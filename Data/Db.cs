using System;
using System.IO;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

namespace PosPokemon.App.Data;

public sealed class Db
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public Db(string dbFileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = Path.Combine(baseDir, dbFileName);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            ForeignKeys = true
        }.ToString();
    }

    public SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void EnsureCreated()
    {
        if (!File.Exists(_dbPath))
        {
            // crea el archivo vacío
            using var _ = File.Create(_dbPath);
        }

        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Schema.sqlite.sql");
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException("No se encontró Schema.sql", schemaPath);

        var sql = File.ReadAllText(schemaPath, Encoding.UTF8);

        using var conn = OpenConnection();
        conn.Execute(sql);
    }
}
