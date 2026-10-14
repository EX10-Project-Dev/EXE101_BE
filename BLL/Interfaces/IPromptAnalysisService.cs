using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Common;

namespace BLL.Interfaces;

public interface IPromptAnalysisService
{
    Task<ParsedSearchCriteria> AnalyzePromptAsync(string prompt, CancellationToken cancellationToken = default);
}
