using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SSISDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncReportsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SyncReportsController> _logger;

        public SyncReportsController(HttpClient httpClient, ILogger<SyncReportsController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
            var apiUrl = "http://localhost:8000/run-powershell"; 


            var payload = new { script_path = @"E:\scripts\sync_reports_from_ssrs.ps1" };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(responseContent);
                }
                else
                {
                    return StatusCode(500, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при вызове FastAPI.");
                return StatusCode(500, new { Status = "Exception", Message = ex.Message });
            }
        }
    }
}

