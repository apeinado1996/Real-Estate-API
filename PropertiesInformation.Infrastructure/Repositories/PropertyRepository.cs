using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.Infrastructure.Repositories
{
    public sealed class PropertyRepository : IPropertyRepository
    {
        private readonly string _cs;
        public PropertyRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("RealEstateDB") ?? throw new InvalidOperationException("Missing connection string 'RealEstateDB'.");
        }

        public async Task<int> AddAsync(Property p, CancellationToken ct = default)
        {
            const string proc = "dbo.Property_Add";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = p.Name });
                cmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)p.Address ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = p.Price });
                cmd.Parameters.Add(new SqlParameter("@CodeInternal", SqlDbType.NVarChar, 50) { Value = p.CodeInternal });
                cmd.Parameters.Add(new SqlParameter("@Year", SqlDbType.SmallInt) { Value = (object?)p.Year ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@IdOwner", SqlDbType.Int) { Value = p.IdOwner });

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

        public async Task UpdateAsync(Property p, CancellationToken ct = default)
        {
            const string proc = "dbo.Property_Update";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = p.Id });
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = p.Name });
                cmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)p.Address ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = p.Price });
                cmd.Parameters.Add(new SqlParameter("@Year", SqlDbType.SmallInt) { Value = (object?)p.Year ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@IdOwner", SqlDbType.Int) { Value = p.IdOwner });

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
            const string proc = "dbo.Property_Delete";
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

        public async Task<Property?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string proc = "dbo.Property_GetById";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                Property? p = null;
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

                await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct))
                {
                    if (await reader.ReadAsync(ct))
                        p = Map(reader);
                }

                await tx.CommitAsync(ct);
                return p;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IReadOnlyList<Property>> ListAsync(string? codeInternal, int? ownerId, decimal? minPrice, decimal? maxPrice, string? address, int? year, string? name, CancellationToken ct = default)
        {
            const string proc = "dbo.Property_List";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var list = new List<Property>();
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@CodeInternal", SqlDbType.NVarChar, 50) { Value = (object?)codeInternal ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@OwnerId", SqlDbType.Int) { Value = (object?)ownerId ?? DBNull.Value });
                var pMin = new SqlParameter("@MinPrice", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = (object?)minPrice ?? DBNull.Value };
                var pMax = new SqlParameter("@MaxPrice", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = (object?)maxPrice ?? DBNull.Value };
                cmd.Parameters.Add(pMin);
                cmd.Parameters.Add(pMax);
                cmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)address ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = (object?)year ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = (object?)name ?? DBNull.Value });

                await using (var reader = await cmd.ExecuteReaderAsync(ct))
                {
                    while (await reader.ReadAsync(ct))
                        list.Add(Map(reader));
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

        public async Task ChangePriceAsync(int idProperty, decimal newPrice, string traceName, decimal traceTax, CancellationToken ct = default)
        {
            const string proc = "dbo.Property_ChangePrice";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@IdProperty", SqlDbType.Int) { Value = idProperty });
                cmd.Parameters.Add(new SqlParameter("@NewPrice", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = newPrice });
                cmd.Parameters.Add(new SqlParameter("@TraceName", SqlDbType.NVarChar, 150) { Value = (object?)traceName ?? "PRICE UPDATE" });
                cmd.Parameters.Add(new SqlParameter("@TraceTax", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = traceTax });

                await cmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
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

        private static Property Map(IDataRecord r) => new()
        {
            Id = (int)r["Id"],
            Name = (string)r["Name"],
            Address = r["Address"] is DBNull ? null : (string)r["Address"],
            Price = (decimal)r["Price"],
            CodeInternal = (string)r["CodeInternal"],
            Year = r["Year"] is DBNull ? (short?)null : Convert.ToInt16(r["Year"]),
            IdOwner = (int)r["IdOwner"]
        };
    }
}
