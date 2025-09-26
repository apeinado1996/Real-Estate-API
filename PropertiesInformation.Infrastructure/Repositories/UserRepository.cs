using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("RealEstateDB") ?? throw new InvalidOperationException("Missing connection string 'RealEstateDB'.");
        }

        public async Task<User> AddAsync(User user, CancellationToken ct = default)
        {
            const string proc = "dbo.Users_Add";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            try
            {
                using var cmd = CreateSpCommand(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 100) { Value = user.UserName });
                cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 200) { Value = user.Email });
                cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = user.PasswordHash });
                cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });
                cmd.Parameters.Add(new SqlParameter("@Rol", SqlDbType.NVarChar, 100) { Value = user.Rol });

                var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                user.Id = Convert.ToInt32(scalar);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return user;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
        
        public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string proc = "dbo.Users_GetById";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            try
            {
                using var cmd = CreateSpCommand(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct).ConfigureAwait(false);
                if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                    return null;
                }

                var user = MapUser(reader);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return user;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        public Task<User?> GetByUserNameAsync(string normalizedUserName, CancellationToken ct = default)
            => GetUserBySingleParamAsync(
                "dbo.Users_GetByNormalizedUserName",
                new SqlParameter("@NormalizedUserName", SqlDbType.NVarChar, 100) { Value = normalizedUserName },                
                ct);

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default)
            => GetUserBySingleParamAsync(
                "dbo.Users_GetByNormalizedEmail",
                new SqlParameter("@NormalizedEmail", SqlDbType.NVarChar, 200) { Value = normalizedEmail },
                ct);
        
        public async Task<bool> ExistsUserNameAsync(string normalizedUserName, CancellationToken ct = default)
        {
            const string proc = "dbo.Users_ExistsByNormalizedUserName";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            try
            {
                using var cmd = CreateSpCommand(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@NormalizedUserName", SqlDbType.NVarChar, 100) { Value = normalizedUserName });

                var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                var exists = Convert.ToBoolean(scalar);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return exists;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<bool> ExistsEmailAsync(string normalizedEmail, CancellationToken ct = default)
        {
            const string proc = "dbo.Users_ExistsByNormalizedEmail";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            try
            {
                using var cmd = CreateSpCommand(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@NormalizedEmail", SqlDbType.NVarChar, 200) { Value = normalizedEmail });

                var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                var exists = Convert.ToBoolean(scalar);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return exists;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
      
        public async Task UpdateAsync(User user, CancellationToken ct = default)
        {
            const string proc = "dbo.Users_Update";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            try
            {
                using var cmd = CreateSpCommand(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = user.Id });
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 100) { Value = user.UserName });
                cmd.Parameters.Add(new SqlParameter("@NormalizedUserName", SqlDbType.NVarChar, 100) { Value = user.NormalizedUserName });
                cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 200) { Value = user.Email });
                cmd.Parameters.Add(new SqlParameter("@NormalizedEmail", SqlDbType.NVarChar, 200) { Value = user.NormalizedEmail });
                cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = user.PasswordHash });
                cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });
                cmd.Parameters.Add(new SqlParameter("@Rol", SqlDbType.NVarChar, 100) { Value = user.Rol });

                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
       
        private static SqlCommand CreateSpCommand(SqlConnection conn, SqlTransaction tx, string spName)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 30;
            return cmd;
        }

        private static User MapUser(IDataRecord r) => new()
        {
            Id = (int)r["Id"],
            UserName = (string)r["UserName"],
            Email = (string)r["Email"],
            PasswordHash = (string)r["PasswordHash"],
            IsActive = (bool)r["IsActive"],
            Rol = (string)r["Rol"],
        };

        private async Task<User?> GetUserBySingleParamAsync(string proc, SqlParameter parameter, CancellationToken ct)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            try
            {
                using var cmd = CreateSpCommand(conn, tx, proc);
                cmd.Parameters.Add(parameter);

                User? user = null;

                await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        user = MapUser(reader);
                    }
                }

                if (user is null)
                {
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                    return null;
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return user;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
    }
}
