using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using MyAwesomeMediaManager.Data;

namespace MyAwesomeMediaManager.Data
{
    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media.db");

        public static void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS media (
                    file_path TEXT PRIMARY KEY,
                    rating INTEGER,
                    favorite INTEGER,
                    tags TEXT
                );
                CREATE TABLE IF NOT EXISTS folders (
                    path TEXT PRIMARY KEY
                );
            ";
            cmd.ExecuteNonQuery();
        }

        public static void AddFolder(string folderPath)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO folders (path) VALUES (@path)";
            command.Parameters.AddWithValue("@path", folderPath);
            command.ExecuteNonQuery();
        }

        public static void InsertOrUpdateMedia(string path)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO media (file_path, rating, favorite, tags)
                VALUES (@path, 0, 0, '')
                ON CONFLICT(file_path) DO NOTHING;
            ";
            command.Parameters.AddWithValue("@path", path);
            command.ExecuteNonQuery();
        }

        public static void UpdateMediaMetadata(string path, int rating, bool favorite, string tags)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE media
                SET rating = @rating,
                    favorite = @favorite,
                    tags = @tags
                WHERE file_path = @path;
            ";
            command.Parameters.AddWithValue("@path", path);
            command.Parameters.AddWithValue("@rating", rating);
            command.Parameters.AddWithValue("@favorite", favorite ? 1 : 0);
            command.Parameters.AddWithValue("@tags", tags);
            command.ExecuteNonQuery();
        }

        public static MediaMetadata? GetMediaMetadata(string path)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT rating, favorite, tags FROM media WHERE file_path = @path";
            command.Parameters.AddWithValue("@path", path);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new MediaMetadata
                {
                    Rating = reader.GetInt32(0),
                    Favorite = reader.GetInt32(1) == 1,
                    Tags = reader.GetString(2)
                };
            }

            return null;
        }

        public static List<string> GetAllMedia()
        {
            var list = new List<string>();
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT file_path FROM media";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }

        public static List<string> GetAllFolders()
        {
            var list = new List<string>();
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT path FROM folders";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }
        public static void DeleteMedia(string path)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM media WHERE file_path = @path";
            command.Parameters.AddWithValue("@path", path);
            command.ExecuteNonQuery();
        }
        public static void ClearSavedFolders()
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM folders";
            cmd.ExecuteNonQuery();
        }
    }
}