using SnapClip.Models;

namespace SnapClip.Services;

/// <summary>
/// Provides fast, ranked full-text search across clipboard history.
/// Maintains an in-memory index for sub-50ms search performance.
/// </summary>
public sealed class SearchService
{
    private readonly object _lock = new();
    private List<ClipItem> _index = [];

    /// <summary>
    /// Rebuilds the in-memory search index with the given clips.
    /// </summary>
    public void RebuildIndex(IEnumerable<ClipItem> clips)
    {
        lock (_lock)
        {
            _index = clips.ToList();
        }
    }

    /// <summary>
    /// Adds a clip to the search index.
    /// </summary>
    public void AddToIndex(ClipItem clip)
    {
        lock (_lock)
        {
            _index.Insert(0, clip);
        }
    }

    /// <summary>
    /// Removes a clip from the search index.
    /// </summary>
    public void RemoveFromIndex(int clipId)
    {
        lock (_lock)
        {
            _index.RemoveAll(c => c.Id == clipId);
        }
    }

    /// <summary>
    /// Searches clips by query string with relevance ranking.
    /// Returns results ordered by: exact match > starts-with > contains, then by recency.
    /// </summary>
    public List<ClipItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAll();

        string queryLower = query.ToLowerInvariant();

        List<ClipItem> snapshot;
        lock (_lock)
        {
            snapshot = [.. _index];
        }

        var scored = new List<(ClipItem Clip, int Score)>();

        foreach (var clip in snapshot)
        {
            int score = ScoreMatch(clip, queryLower);
            if (score > 0)
            {
                scored.Add((clip, score));
            }
        }

        return scored
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Clip.CapturedAt)
            .Select(s => s.Clip)
            .ToList();
    }

    /// <summary>
    /// Returns all indexed clips ordered by pinned status then recency.
    /// </summary>
    public List<ClipItem> GetAll()
    {
        lock (_lock)
        {
            return _index
                .OrderByDescending(c => c.IsPinned)
                .ThenByDescending(c => c.CapturedAt)
                .ToList();
        }
    }

    private static int ScoreMatch(ClipItem clip, string queryLower)
    {
        int bestScore = 0;

        bestScore = Math.Max(bestScore, ScoreField(clip.Content, queryLower));
        bestScore = Math.Max(bestScore, ScoreField(clip.Category, queryLower));
        bestScore = Math.Max(bestScore, ScoreField(clip.SourceApplication, queryLower));

        // Boost pinned items slightly
        if (bestScore > 0 && clip.IsPinned)
            bestScore += 1;

        return bestScore;
    }

    private static int ScoreField(string? field, string queryLower)
    {
        if (string.IsNullOrEmpty(field))
            return 0;

        string fieldLower = field.ToLowerInvariant();

        // Exact match
        if (fieldLower == queryLower)
            return 100;

        // Starts with
        if (fieldLower.StartsWith(queryLower, StringComparison.Ordinal))
            return 75;

        // Contains
        if (fieldLower.Contains(queryLower, StringComparison.Ordinal))
            return 50;

        return 0;
    }
}
