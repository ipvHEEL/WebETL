using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.Data;




[ApiController]
[Route("api/[controller]")]
public class ChechCollisions : ControllerBase
{
    private readonly string _ConnectionStrings;


    public ChechCollisions(IConfiguration configuration) 
    {
        _ConnectionStrings = configuration.GetConnectionString("SsisDb");
    }

    [HttpGet("job-log")]
    public async Task<IActionResult> GetJobLog()
    {
        var jobname = "ETLjob";
        var logs = new List<JobLogEntry>();

        using var connection = new SqlConnection(_ConnectionStrings);
        await connection.OpenAsync();

        var sql = @"
        SELECT TOP 100
                j.name AS JobName,
                h.step_id,
                h.step_name,
                h.message,
                h.run_status,
                run_date = CAST(
                    CAST(h.run_date AS VARCHAR) + ' ' +
                    STUFF(STUFF(RIGHT('000000' + CAST(h.run_time AS VARCHAR), 6), 3, 0, ':'), 6, 0, ':')
                    AS DATETIME),
                h.run_duration
            FROM msdb.dbo.sysjobs j
            INNER JOIN msdb.dbo.sysjobhistory h ON j.job_id = h.job_id
            WHERE j.name = @JobName
            ORDER BY h.instance_id DESC    
        ";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@JobName", jobname);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new JobLogEntry
            {
                JobName = reader["JobName"].ToString(),
                StepId = Convert.ToInt32(reader["step_id"]),
                StepName = reader["step_name"].ToString(),
                Message = reader["message"].ToString(),
                RunStatus = Convert.ToInt32(reader["run_status"]),
                RunDate = (DateTime)reader["run_date"],
                RunDuration = Convert.ToInt32(reader["run_duration"])
            });
        }

        return Ok(logs);
    }
}

public class JobLogEntry
{
    public string JobName { get; set; } = string.Empty;
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int RunStatus { get; set; } 
    public DateTime RunDate { get; set; }
    public int RunDuration { get; set; }
}
