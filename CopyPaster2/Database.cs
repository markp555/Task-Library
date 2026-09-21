using System;
using System.Collections.Generic;
using System.Text;

namespace CopyPaster2
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;

    public class DatabaseWrapper : IDisposable
    {
        private readonly SqliteConnection _connection;

        public DatabaseWrapper(string connectionString)
        {
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
        }

        /// <summary>
        /// Инициализация структуры БД, таблиц и FTS5 индекса
        /// </summary>
        public async Task InitializeAsync()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
            -- Таблица учетных данных
            CREATE TABLE IF NOT EXISTS Credentials (
                User TEXT PRIMARY KEY,
                Password TEXT NOT NULL
            );

            -- Основная таблица задач
            CREATE TABLE IF NOT EXISTS TaskRecord (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskName TEXT NOT NULL,
                Contest TEXT,
                Username TEXT,
                Submission TEXT,
                Statements TEXT,
                Status INTEGER NOT NULL
            );

            -- Виртуальная таблица FTS5 для полнотекстового поиска
            -- tokenize='unicode61' поддерживает русский и английский языки
            CREATE VIRTUAL TABLE IF NOT EXISTS TaskFts USING fts5(
                TaskName, Contest, Submission, Statements,
                content='TaskRecord',
                content_rowid='Id',
                tokenize='unicode61'
            );

            -- Триггеры для синхронизации FTS индекса с основной таблицей
            CREATE TRIGGER IF NOT EXISTS task_ai AFTER INSERT ON TaskRecord BEGIN
                INSERT INTO TaskFts(rowid, TaskName, Contest, Submission, Statements) 
                VALUES (new.Id, new.TaskName, new.Contest, new.Submission, new.Statements);
            END;

            CREATE TRIGGER IF NOT EXISTS task_ad AFTER DELETE ON TaskRecord BEGIN
                INSERT INTO TaskFts(TaskFts, rowid, TaskName, Contest, Submission, Statements) 
                VALUES('delete', old.Id, old.TaskName, old.Contest, old.Submission, old.Statements);
            END;

            CREATE TRIGGER IF NOT EXISTS task_au AFTER UPDATE ON TaskRecord BEGIN
                INSERT INTO TaskFts(TaskFts, rowid, TaskName, Contest, Submission, Statements) 
                VALUES('delete', old.Id, old.TaskName, old.Contest, old.Submission, old.Statements);
                INSERT INTO TaskFts(rowid, TaskName, Contest, Submission, Statements) 
                VALUES (new.Id, new.TaskName, new.Contest, new.Submission, new.Statements);
            END;
        ";
            await command.ExecuteNonQueryAsync();
        }

        #region Credentials Methods

        public async Task<Credentials?> GetCredentialsByUsernameAsync(string username)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT User, Password FROM Credentials WHERE User = @user";
            command.Parameters.AddWithValue("@user", username);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Credentials(reader.GetString(0), reader.GetString(1));
            }
            return null;
        }

        public async Task<List<string>> GetAllUsernamesAsync()
        {
            var usernames = new List<string>();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT User FROM Credentials";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                usernames.Add(reader.GetString(0));
            }
            return usernames;
        }

        public async Task AddCredentialsAsync(Credentials credentials)
        {
            using var command = _connection.CreateCommand();
            // INSERT OR REPLACE обновит пароль, если пользователь уже существует
            command.CommandText = "INSERT OR REPLACE INTO Credentials (User, Password) VALUES (@user, @password)";
            command.Parameters.AddWithValue("@user", credentials.User);
            command.Parameters.AddWithValue("@password", credentials.Password);

            await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region TaskRecord Methods

        public async Task<long> AddTaskAsync(TaskRecord task)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO TaskRecord (TaskName, Contest, Username, Submission, Statements, Status)
            VALUES (@TaskName, @Contest, @Username, @Submission, @Statements, @Status);
            SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@TaskName", task.TaskName);
            command.Parameters.AddWithValue("@Contest", (object?)task.Contest ?? DBNull.Value);
            command.Parameters.AddWithValue("@Username", (object?)task.Username ?? DBNull.Value);
            command.Parameters.AddWithValue("@Submission", (object?)task.Submission ?? DBNull.Value);
            command.Parameters.AddWithValue("@Statements", (object?)task.Statements ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)task.Status);

            var newId = (long)(await command.ExecuteScalarAsync())!;
            return newId;
        }

        /// <summary>
        /// Поиск задач по имени (TaskName) через FTS5 с опциональным фильтром по статусу OK
        /// </summary>
        public async Task<List<TaskRecord>> SearchTasksAsync(string query, bool onlyOkStatus = false)
        {
            var results = new List<TaskRecord>();

            // Экранируем двойные кавычки для синтаксиса FTS5 и оборачиваем в фразу
            string safeQuery = query.Replace("\"", "\"\"");
            // Указываем FTS5 искать конкретно в колонке TaskName
            string ftsMatch = $"{safeQuery}";

            using var command = _connection.CreateCommand();
            command.CommandText = @"
            SELECT t.Id, t.TaskName, t.Contest, t.Username, t.Submission, t.Statements, t.Status
            FROM TaskRecord t
            JOIN TaskFts f ON t.Id = f.rowid
            WHERE TaskFts MATCH @ftsQuery
              AND (@statusFilter IS NULL OR t.Status = @statusFilter)";

            command.Parameters.AddWithValue("@ftsQuery", ftsMatch);

            // Если фильтр включен, передаем (int)TaskStatus.OK, иначе NULL
            command.Parameters.AddWithValue("@statusFilter", onlyOkStatus ? (object)(int)TaskStatus.OK : DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new TaskRecord(
                    Id: reader.GetInt64(0),
                    TaskName: reader.GetString(1),
                    Contest: reader.GetString(2),
                    Username: reader.GetString(3),
                    Submission: reader.GetString(4),
                    Statements: reader.GetString(5),
                    Status: (TaskStatus)reader.GetInt32(6)
                ));
            }

            return results;
        }

        #endregion

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
