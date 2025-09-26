using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System.Data;

namespace PropertiesInformation.Infrastructure.Repositories
{
    public sealed class PropertyTraceRepository : IPropertyTraceRepository
    {
        private readonly string _cs;
        public PropertyTraceRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("RealEstateDB") ?? throw new InvalidOperationException("Missing connection string 'RealEstateDB'.");
        }

        public async Task<int> AddAsync(PropertyTrace t, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyTrace_Add";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@IdProperty", SqlDbType.Int) { Value = t.IdProperty });
                cmd.Parameters.Add(new SqlParameter("@DateSale", SqlDbType.Date) { Value = t.DateSale.Date });
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = t.Name });
                cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = t.Value });
                cmd.Parameters.Add(new SqlParameter("@Tax", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = t.Tax });

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

        public async Task UpdateAsync(PropertyTrace t, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyTrace_Update";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = t.Id });
                cmd.Parameters.Add(new SqlParameter("@DateSale", SqlDbType.Date) { Value = t.DateSale.Date });
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = t.Name });
                cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = t.Value });
                cmd.Parameters.Add(new SqlParameter("@Tax", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = t.Tax });

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
            const string proc = "dbo.PropertyTrace_Delete";
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

        public async Task<PropertyTrace?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyTrace_GetById";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                PropertyTrace? tr = null;
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

                await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct))
                {
                    if (await reader.ReadAsync(ct))
                        tr = Map(reader);
                }

                await tx.CommitAsync(ct);
                return tr;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IReadOnlyList<PropertyTrace>> ListByPropertyAsync(int idProperty, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyTrace_ListByProperty";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var list = new List<PropertyTrace>();
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@IdProperty", SqlDbType.Int) { Value = idProperty });
                cmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.Date) { Value = (object?)fromDate?.Date ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.Date) { Value = (object?)toDate?.Date ?? DBNull.Value });

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

        public async Task<(int CountTraces, decimal TotalValue, decimal TotalTax)> SummaryByPropertyAsync(int idProperty, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
        {
            const string proc = "dbo.PropertyTrace_SummaryByProperty";
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                using var cmd = CreateSp(conn, tx, proc);
                cmd.Parameters.Add(new SqlParameter("@IdProperty", SqlDbType.Int) { Value = idProperty });
                cmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.Date) { Value = (object?)fromDate?.Date ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.Date) { Value = (object?)toDate?.Date ?? DBNull.Value });

                int count = 0; decimal totalValue = 0m; decimal totalTax = 0m;
                await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct))
                {
                    if (await reader.ReadAsync(ct))
                    {
                        count = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        totalValue = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                        totalTax = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                    }
                }

                await tx.CommitAsync(ct);
                return (count, totalValue, totalTax);
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

        private static PropertyTrace Map(IDataRecord r) => new()
        {
            Id = (int)r["Id"],
            DateSale = (DateTime)r["DateSale"],
            Name = (string)r["Name"],
            Value = (decimal)r["Value"],
            Tax = (decimal)r["Tax"],
            IdProperty = (int)r["IdProperty"]
        };
    }
}
