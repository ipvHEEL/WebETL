// Controllers/ETLController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


[ApiController]
[Route("api/[controller]")]
public class ETLController : ControllerBase
{
    private readonly string _connStr;

    public ETLController(IConfiguration config)
    {
        _connStr = config.GetConnectionString("SsisDb");
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        var cmd = new SqlCommand(@"
            SELECT TOP 10 
    e.execution_id,
    e.package_name,
    e.start_time,
    e.end_time,
    e.status,
    CASE CAST(e.status AS INT)
        WHEN 2 THEN 'Running'
        WHEN 4 THEN 'Success'
        WHEN 5 THEN 'Failed'
        WHEN 6 THEN 'Pending'
        WHEN 3 THEN 'Canceled'
        WHEN 7 THEN 'Stopped'
        ELSE 'Unknown (' + CAST(e.status AS VARCHAR(10)) + ')'
    END AS status_text,
    'Вручную' AS triggered_by
FROM SSISDB.catalog.executions e
WHERE e.package_name = @PackageName
  AND e.project_name = 'ETLProcess'  
ORDER BY e.start_time DESC", conn);

        cmd.Parameters.AddWithValue("@PackageName", "Package.dtsx");

        var results = new List<Dictionary<string, object>>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Dictionary<string, object>
            {
                ["id"] = reader["execution_id"],
                ["package"] = reader["package_name"],
                ["status"] = reader["status_text"],
                ["start"] = reader["start_time"],
                ["end"] = reader["end_time"] == DBNull.Value ? null : reader["end_time"],
                ["triggered_by"] = reader["triggered_by"].ToString()
            });
        }
        return Ok(results);
    }
}