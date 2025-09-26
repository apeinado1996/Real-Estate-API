using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System.Data;

namespace PropertiesInformation.Infrastructure.Repositories
{
    public sealed class OwnerRepository : IOwnerRepository
    {
        private readonly string _cs;
        public OwnerRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("RealEstateDB") ?? throw new InvalidOperationException("Missing connection string 'RealEstateDB'.");
        }

        public async Task<int> AddAsync(Owner o, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_Add";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = o.Name });
                cmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)o.Address ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Birthday", SqlDbType.Date) { Value = (object?)o.Birthday?.Date ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Photo", SqlDbType.VarBinary, -1) { Value = (object?)o.Photo ?? DBNull.Value });

                var idObj = await cmd.ExecuteScalarAsync(ct);
                var id = Convert.ToInt32(idObj);

                await tx.CommitAsync(ct);
                return id;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task UpdateAsync(Owner o, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_Update";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = o.Id });
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = o.Name });
                cmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)o.Address ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Birthday", SqlDbType.Date) { Value = (object?)o.Birthday?.Date ?? DBNull.Value });

                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_Delete";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<Owner?> GetByIdAsync(int id, bool includePhoto = false, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_GetById";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                Owner? owner = null;
                using (var cmd = CreateSp(conn, tx, proc))
                {
                    cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                    await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
                    if (await reader.ReadAsync(ct))
                    {
                        owner = new Owner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                            Birthday = reader.IsDBNull(reader.GetOrdinal("Birthday")) ? null : reader.GetDateTime(reader.GetOrdinal("Birthday"))
                        };
                    }
                }

                if (owner is null)
                {
                    await tx.CommitAsync(ct);
                    return null;
                }

                if (includePhoto)
                {
                    using var cmdPhoto = CreateSp(conn, tx, "dbo.Owner_GetPhoto");
                    cmdPhoto.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                    await using var readerPhoto = await cmdPhoto.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
                    if (await readerPhoto.ReadAsync(ct) && !readerPhoto.IsDBNull(0))
                        owner.Photo = (byte[])readerPhoto[0];
                }

                await tx.CommitAsync(ct);
                return owner;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IReadOnlyList<Owner>> ListAsync(string? search, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_List";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var list = new List<Owner>();
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 150) { Value = (object?)search ?? DBNull.Value });

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    list.Add(new Owner
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                        Birthday = reader.IsDBNull(reader.GetOrdinal("Birthday")) ? null : reader.GetDateTime(reader.GetOrdinal("Birthday"))
                    });
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

        public async Task UpdatePhotoAsync(int id, byte[] photo, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_UpdatePhoto";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                cmd.Parameters.Add(new SqlParameter("@Photo", SqlDbType.VarBinary, -1) { Value = photo });
                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<byte[]?> GetPhotoAsync(int id, CancellationToken ct = default)
        {
            const string proc = "dbo.Owner_GetPhoto";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                byte[]? photo = null;
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
                if (await reader.ReadAsync(ct) && !reader.IsDBNull(0))
                    photo = (byte[])reader[0];

                await tx.CommitAsync(ct);
                return photo;
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
