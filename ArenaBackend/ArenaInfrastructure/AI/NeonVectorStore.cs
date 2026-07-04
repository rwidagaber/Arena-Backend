using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

namespace ArenaInfrastructure.AI
{
    /// <summary>
    /// Raw-SQL vector store backed by Neon PostgreSQL + pgvector.
    /// Uses Npgsql directly (no EF Core provider) to avoid version compatibility issues.
    /// The NpgsqlDataSource is registered as a singleton in DI so connections are pooled.
    /// </summary>
    public class NeonVectorStore
    {
        private readonly NpgsqlDataSource _dataSource;

        public NeonVectorStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        // ── Schema Init ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the MemberHealthVectors table and HNSW index if they do not exist.
        /// Called once on application startup — idempotent and safe to re-run.
        /// </summary>
        public async Task EnsureSchemaAsync()
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            // 1. Create the pgvector extension + table
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    CREATE EXTENSION IF NOT EXISTS vector;

                    CREATE TABLE IF NOT EXISTS "MemberHealthVectors" (
                        "Id"              uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
                        "MemberProfileId" uuid          NOT NULL,
                        "Content"         varchar(1000) NOT NULL,
                        "Category"        varchar(100)  NOT NULL,
                        "Embedding"       vector(768),
                        "RecordedAt"      timestamptz   NOT NULL,
                        "IsActive"        boolean       NOT NULL DEFAULT true
                    );

                    CREATE INDEX IF NOT EXISTS "ix_mhv_member_profile_id"
                        ON "MemberHealthVectors" ("MemberProfileId");
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. HNSW index — requires pgvector >= 0.5 (Neon supports it)
            try
            {
                await using var idxCmd = conn.CreateCommand();
                idxCmd.CommandText = """
                    CREATE INDEX IF NOT EXISTS "ix_mhv_embedding_hnsw"
                        ON "MemberHealthVectors"
                        USING hnsw ("Embedding" vector_cosine_ops)
                        WITH (m = 16, ef_construction = 64);
                    """;
                await idxCmd.ExecuteNonQueryAsync();
                Console.WriteLine("[VectorStore] ✅ HNSW index ready on Neon PostgreSQL");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VectorStore] ⚠️ HNSW index skipped: {ex.Message}");
            }
        }

        // ── Write ─────────────────────────────────────────────────────────────────

        public async Task<bool> ExistsAsync(Guid memberProfileId, string content)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(1) FROM "MemberHealthVectors"
                WHERE "MemberProfileId" = $1 AND "Content" = $2 AND "IsActive" = true
                """;
            cmd.Parameters.AddWithValue(memberProfileId);
            cmd.Parameters.AddWithValue(content);
            var count = (long)(await cmd.ExecuteScalarAsync())!;
            return count > 0;
        }

        public async Task InsertAsync(Guid memberProfileId, string content, string category, float[]? embedding)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO "MemberHealthVectors"
                    ("Id", "MemberProfileId", "Content", "Category", "Embedding", "RecordedAt", "IsActive")
                VALUES
                    (gen_random_uuid(), $1, $2, $3, $4, NOW() AT TIME ZONE 'UTC', true)
                """;
            cmd.Parameters.AddWithValue(memberProfileId);
            cmd.Parameters.AddWithValue(content);
            cmd.Parameters.AddWithValue(category);

            if (embedding is { Length: > 0 })
                cmd.Parameters.AddWithValue(new Vector(embedding));
            else
            {
                var p = cmd.CreateParameter();
                p.Value = DBNull.Value;
                cmd.Parameters.Add(p);
            }

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteByCategoryAsync(Guid memberProfileId, string category)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE "MemberHealthVectors"
                SET "IsActive" = false
                WHERE "MemberProfileId" = $1 AND "Category" = $2
                """;
            cmd.Parameters.AddWithValue(memberProfileId);
            cmd.Parameters.AddWithValue(category);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteByKeywordAsync(Guid memberProfileId, string category, string keyword)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE "MemberHealthVectors"
                SET "IsActive" = false
                WHERE "MemberProfileId" = $1 
                  AND "Category" = $2 
                  AND LOWER("Content") LIKE $3
                """;
            cmd.Parameters.AddWithValue(memberProfileId);
            cmd.Parameters.AddWithValue(category);
            cmd.Parameters.AddWithValue($"%{keyword.ToLowerInvariant()}%");
            await cmd.ExecuteNonQueryAsync();
        }

        // ── Read / Search ─────────────────────────────────────────────────────────

        /// <summary>
        /// ANN cosine similarity search using the pgvector <=> operator + HNSW index.
        /// Returns top-K results ordered by ascending cosine DISTANCE (0 = identical).
        /// This runs entirely inside PostgreSQL — no C# loops, scales to millions of rows.
        /// </summary>
        public async Task<List<(string Content, double Distance)>> SearchBySimilarityAsync(
            Guid memberProfileId, float[] queryEmbedding, int topK = 5)
        {
            var queryVector = new Vector(queryEmbedding);
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT "Content", "Embedding" <=> $1 AS distance
                FROM   "MemberHealthVectors"
                WHERE  "MemberProfileId" = $2
                  AND  "IsActive"        = true
                  AND  "Embedding"       IS NOT NULL
                ORDER BY "Embedding" <=> $1
                LIMIT $3
                """;
            cmd.Parameters.AddWithValue(queryVector);
            cmd.Parameters.AddWithValue(memberProfileId);
            cmd.Parameters.AddWithValue(topK);

            var results = new List<(string Content, double Distance)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add((reader.GetString(0), reader.GetDouble(1)));

            return results;
        }

        /// <summary>
        /// Returns all active content rows matching one of the critical category names.
        /// Always included in context regardless of similarity score.
        /// </summary>
        public async Task<List<string>> GetCriticalContentAsync(Guid memberProfileId, string[] categories)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT DISTINCT "Content"
                FROM   "MemberHealthVectors"
                WHERE  "MemberProfileId" = $1
                  AND  "IsActive"        = true
                  AND  "Category"        = ANY($2)
                """;
            cmd.Parameters.AddWithValue(memberProfileId);
            cmd.Parameters.AddWithValue(categories);

            var results = new List<string>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(reader.GetString(0));
            return results;
        }
    }
}
