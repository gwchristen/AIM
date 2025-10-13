using AIM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

public interface ILoggingService
{
    Task LogAsync(string action, string details = "");
    Task<IEnumerable<LogEntry>> GetLogsAsync();
}