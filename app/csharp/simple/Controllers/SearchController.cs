using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Azure.Search.Documents;

using Simple.Services;

/// <summary>
/// 検索関連のリクエストを処理するコントローラー。
/// </summary>
[ApiController]
[Route("[controller]")]
public class SearchController : ControllerBase
{
    private readonly AoaiService _aoaiService;
    private readonly AiSearchService _aiSearchService;

    /// <summary>
    /// <see cref="SearchController"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="aoaiService">AOAIサービス。</param>
    /// <param name="aiSearchService">AI検索サービス。</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="aoaiService"/> または <paramref name="aiSearchService"/> がnullの場合にスローされます。
    /// </exception>
    public SearchController(AoaiService aoaiService, AiSearchService aiSearchService)
    {
        _aoaiService = aoaiService ?? throw new ArgumentNullException(nameof(aoaiService));
        _aiSearchService = aiSearchService ?? throw new ArgumentNullException(nameof(aiSearchService));
    }

    /// <summary>
    /// フルテキスト検索を実行します。
    /// </summary>
    /// <param name="query">検索クエリ。</param>
    /// <returns>検索結果を含む <see cref="IActionResult"/>。</returns>
    [HttpGet("fulltext")]
    public async Task<IActionResult> FullTextSearch(string query)
    {
        try
        {
            var rewrittenQuery = await _aoaiService.RewriteQueryAsync(query);
            var results = _aiSearchService.FullTextSearch(rewrittenQuery);
            var response = await _aoaiService.GenerateAnswerAsync(query, results);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// ベクトル検索を実行します。
    /// </summary>
    /// <param name="query">検索クエリ。</param>
    /// <returns>検索結果を含む <see cref="IActionResult"/>。</returns>
    [HttpGet("vector")]
    public async Task<IActionResult> VectorSearch(string query)
    {
        try
        {
            var rewrittenQuery = await _aoaiService.RewriteQueryAsync(query);
            ReadOnlyMemory<float> vectorizedQuery = _aoaiService.GetVectorizedQuery(rewrittenQuery);
            var results = _aiSearchService.VectorSearch(vectorizedQuery);
            var response = await _aoaiService.GenerateAnswerAsync(query, results);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}