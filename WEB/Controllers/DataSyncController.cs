// Controllers/DataSyncController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

[ApiController]
[Route("api/[controller]")]
public class DataSyncController : ControllerBase
{
    private readonly string _ssisConnStr;
    private readonly string _dwhConnStr;
    private readonly string _erpConnStr;

    public DataSyncController(IConfiguration configuration)
    {
        _ssisConnStr = configuration.GetConnectionString("SsisDb")
                       ?? throw new InvalidOperationException("Connection string 'SsisDb' not found.");

        _dwhConnStr = configuration.GetConnectionString("DwhDb")
                      ?? throw new InvalidOperationException("Connection string 'DwhDb' not found.");

        _erpConnStr = configuration.GetConnectionString("ErpDb")
                      ?? throw new InvalidOperationException("Connection string 'ErpDb' not found.");
    }

    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
             
            long? lastExecutionId = null;
            DateTime? lastEndTime = null;
            using (var conn = new SqlConnection(_ssisConnStr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                SELECT TOP 1 execution_id, end_time
                FROM SSISDB.catalog.executions
                WHERE package_name = 'ETLPackage.dtsx'
                  AND project_name = 'ETLProd'
                  AND status = 4
                ORDER BY end_time DESC", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {

                    lastExecutionId = Convert.ToInt64(reader["execution_id"]);

                    if (reader["end_time"] != DBNull.Value)
                    {

                        var dto = (DateTimeOffset)reader["end_time"];
                        lastEndTime = dto.DateTime;
                    }
                }
            }
            DateTime localDate = DateTime.Now;
            int MonthValue = localDate.Month - 1;
            int YearValue = localDate.Year;
            string stringMonthValue = Convert.ToString(localDate.Month);
            string stringYearValue = Convert.ToString(localDate.Year);

            long dwhCount = 0;
            long erpCount = 0;
            long sumDwhCount = 0;
            long sumErpCount = 0;
            for (int i = MonthValue; i > 0; i--)
            {

                string CorrentStringMonthValue;

                if (i < 10)
                {
                    CorrentStringMonthValue = '0' + Convert.ToString(i);
                }
                else
                {
                    CorrentStringMonthValue = Convert.ToString(i); 
                }


                using (var conn = new SqlConnection(_dwhConnStr))
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM cp_DWH_LA0052_" + stringYearValue + CorrentStringMonthValue, conn);
                    dwhCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    sumDwhCount += dwhCount;
                }


                
                using (var conn = new SqlConnection(_erpConnStr))
                {
                    await conn.OpenAsync(); //[la0052_001_202512_ls10]
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM [dbo].[la0052_001_" + stringYearValue + CorrentStringMonthValue + "_ls10]", conn);
                    erpCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    sumErpCount += erpCount;
                }

                
            }

                bool isSynced = sumErpCount == sumDwhCount;

            return Ok(new
            {
                isSynced,
                sumErpCount,
                sumDwhCount,
                difference = Math.Abs(sumErpCount - sumDwhCount),
                lastEtlExecutionId = lastExecutionId,
                lastEtlEndTime = lastEndTime,
                message = isSynced
                    ? "Все данные синхронизированы"
                    : $"Расхождение: ERP={sumErpCount}, DWH={sumDwhCount}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}