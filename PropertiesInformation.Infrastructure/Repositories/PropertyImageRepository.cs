using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System.Data;

namespace PropertiesInformation.Infrastructure.Repositories
{
    public sealed class PropertyImageRepository : IPropertyImageRepository
    {
        private readonly string _cs;
        public PropertyImageRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("RealEstateDB") ?? throw new InvalidOperationException("Missing connection string 'RealEstateDB'.");
        }

        public async Task<int> AddAsync(int idProperty, byte[] fileBytes, string? fileName, string? contentType, bool enabled, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_Add";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@IdProperty", SqlDbType.Int) { Value = idProperty });
                cmd.Parameters.Add(new SqlParameter("@File", SqlDbType.VarBinary, -1) { Value = fileBytes });
                cmd.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 255) { Value = (object?)fileName ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ContentType", SqlDbType.NVarChar, 100) { Value = (object?)contentType ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Enabled", SqlDbType.Bit) { Value = enabled });

                var scalar = await cmd.ExecuteScalarAsync(ct);
                var newId = Convert.ToInt32(scalar);
                await tx.CommitAsync(ct);
                return newId;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task ReplaceFileAsync(int idImage, byte[] fileBytes, string? fileName, string? contentType, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_UpdateFile";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = idImage });
                cmd.Parameters.Add(new SqlParameter("@File", SqlDbType.VarBinary, -1) { Value = fileBytes });
                cmd.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 255) { Value = (object?)fileName ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ContentType", SqlDbType.NVarChar, 100) { Value = (object?)contentType ?? DBNull.Value });

                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task SetEnabledAsync(int idImage, bool enabled, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_SetEnabled";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = idImage });
                cmd.Parameters.Add(new SqlParameter("@Enabled", SqlDbType.Bit) { Value = enabled });
                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task DeleteAsync(int idImage, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_Delete";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = idImage });
                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<PropertyImage?> GetByIdAsync(int idImage, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_GetById";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                PropertyImage? img = null;
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = idImage });

                await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct))
                {
                    if (await reader.ReadAsync(ct))
                    {
                        img = new PropertyImage
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            IdProperty = reader.GetInt32(reader.GetOrdinal("IdProperty")),
                            FileName = reader.IsDBNull(reader.GetOrdinal("FileName")) ? null : reader.GetString(reader.GetOrdinal("FileName")),
                            ContentType = reader.IsDBNull(reader.GetOrdinal("ContentType")) ? null : reader.GetString(reader.GetOrdinal("ContentType")),
                            Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled"))
                        };
                    }
                }

                await tx.CommitAsync(ct);
                return img;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<(byte[]? bytes, string? fileName, string? contentType)> GetFileAsync(int idImage, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_GetFile";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                byte[]? bytes = null;
                string? fileName = null;
                string? contentType = null;

                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = idImage });

                await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct))
                {
                    if (await reader.ReadAsync(ct))
                    {
                        if (!reader.IsDBNull(0)) bytes = (byte[])reader[0];
                        if (!reader.IsDBNull(1)) fileName = reader.GetString(1);
                        if (!reader.IsDBNull(2)) contentType = reader.GetString(2);
                    }
                }

                await tx.CommitAsync(ct);
                return (bytes, fileName, contentType);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IReadOnlyList<PropertyImage>> ListByPropertyAsync(int idProperty, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyImage_ListByProperty";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var list = new List<PropertyImage>();
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@IdProperty", SqlDbType.Int) { Value = idProperty });

                await using (var reader = await cmd.ExecuteReaderAsync(ct))
                {
                    while (await reader.ReadAsync(ct))
                    {
                        list.Add(new PropertyImage
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            IdProperty = reader.GetInt32(reader.GetOrdinal("IdProperty")),
                            FileName = reader.IsDBNull(reader.GetOrdinal("FileName")) ? null : reader.GetString(reader.GetOrdinal("FileName")),
                            ContentType = reader.IsDBNull(reader.GetOrdinal("ContentType")) ? null : reader.GetString(reader.GetOrdinal("ContentType")),
                            Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled"))
                        });
                    }
                }

                await tx.CommitAsync(ct);
                return list;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private static SqlCommand CreateSp(SqlConnection conn, SqlTransaction tx, string sp)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = sp;
            cmd.CommandTimeout = 30;
            return cmd;
        }
    }
}
