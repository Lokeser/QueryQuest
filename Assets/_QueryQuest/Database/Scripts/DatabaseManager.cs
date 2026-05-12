// Assets/_QueryQuest/Database/Scripts/DatabaseManager.cs
using System;
using System.IO;
using SQLite;
using UnityEngine;
using QueryQuest.Models;

namespace QueryQuest.Database
{
    /// <summary>
    /// Singleton que gerencia a conexão com o banco SQLite local.
    /// Persiste entre cenas via DontDestroyOnLoad.
    /// </summary>
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager Instance { get; private set; }

        private SQLiteConnection _db;
        public SQLiteConnection DB => _db;

        // Nome do arquivo .db em StreamingAssets (editor) e persistentDataPath (build)
        private const string DB_NAME = "queryquest.db";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string dbPath = GetDatabasePath();

            try
            {
                _db = new SQLiteConnection(dbPath);
                Debug.Log($"[DatabaseManager] Conectado ao banco: {dbPath}");

                // Cria tabelas se não existirem (seguro chamar sempre)
                _db.CreateTable<SpellData>();
                _db.CreateTable<EnemyData>();

                // Popula dados iniciais se o banco estiver vazio
                DatabaseSeeder.SeedIfEmpty(_db);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] Falha ao inicializar banco: {e.Message}");
            }
        }

        /// <summary>
        /// No Editor e em builds desktop: copia o .db de StreamingAssets para persistentDataPath
        /// para permitir escrita. Em WebGL não é suportado (jogo é offline/desktop).
        /// </summary>
        private string GetDatabasePath()
        {
#if UNITY_EDITOR
            // No editor, usamos diretamente o StreamingAssets para facilitar edição
            return Path.Combine(Application.streamingAssetsPath, DB_NAME);
#else
            string destPath = Path.Combine(Application.persistentDataPath, DB_NAME);

            // Na primeira execução da build, copia o .db template de StreamingAssets
            if (!File.Exists(destPath))
            {
                string sourcePath = Path.Combine(Application.streamingAssetsPath, DB_NAME);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath);
                    Debug.Log("[DatabaseManager] Banco copiado para persistentDataPath.");
                }
                else
                {
                    Debug.Log("[DatabaseManager] Nenhum banco template encontrado. Criando novo.");
                }
            }

            return destPath;
#endif
        }

        private void OnApplicationQuit()
        {
            _db?.Close();
            Debug.Log("[DatabaseManager] Banco fechado.");
        }
    }
}
