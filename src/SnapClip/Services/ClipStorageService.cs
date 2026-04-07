using Microsoft.EntityFrameworkCore;
using SnapClip.Data;
using SnapClip.Models;

namespace SnapClip.Services;

/// <summary>
/// Provides CRUD operations for clipboard history stored in SQLite.
/// </summary>
public sealed class ClipStorageService
{
    private readonly IDbContextFactory<SnapClipDbContext> _contextFactory;
    private int _maxHistorySize = 1000;

    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set => _maxHistorySize = Math.Max(10, value);
    }

    public ClipStorageService(IDbContextFactory<SnapClipDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Saves a new clip to the database, enforcing the history size limit.
    /// </summary>
    public async Task<ClipItem> SaveClipAsync(ClipItem clip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // Check for duplicate (same content as most recent clip)
        var lastClip = await context.Clips
            .OrderByDescending(c => c.CapturedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (lastClip is not null &&
            lastClip.Content == clip.Content &&
            lastClip.Type == clip.Type)
        {
            return lastClip;
        }

        context.Clips.Add(clip);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await EnforceHistoryLimitAsync(context).ConfigureAwait(false);

        return clip;
    }

    /// <summary>
    /// Retrieves clips ordered by pinned status then capture time (newest first).
    /// </summary>
    public async Task<List<ClipItem>> GetClipsAsync(int limit = 100, int offset = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.Clips
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.CapturedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all pinned clips.
    /// </summary>
    public async Task<List<ClipItem>> GetPinnedClipsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.Clips
            .Where(c => c.IsPinned)
            .OrderByDescending(c => c.CapturedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a clip by ID.
    /// </summary>
    public async Task<ClipItem?> GetClipByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Clips.FindAsync(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a clip from the database.
    /// </summary>
    public async Task DeleteClipAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var clip = await context.Clips.FindAsync(id).ConfigureAwait(false);
        if (clip is not null)
        {
            context.Clips.Remove(clip);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Toggles the pinned state of a clip.
    /// </summary>
    public async Task TogglePinAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var clip = await context.Clips.FindAsync(id).ConfigureAwait(false);
        if (clip is not null)
        {
            clip.IsPinned = !clip.IsPinned;
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Toggles the favorite state of a clip.
    /// </summary>
    public async Task ToggleFavoriteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var clip = await context.Clips.FindAsync(id).ConfigureAwait(false);
        if (clip is not null)
        {
            clip.IsFavorite = !clip.IsFavorite;
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Records a paste event for a clip.
    /// </summary>
    public async Task RecordPasteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var clip = await context.Clips.FindAsync(id).ConfigureAwait(false);
        if (clip is not null)
        {
            clip.PasteCount++;
            clip.LastPastedAt = DateTime.UtcNow;
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Updates the encrypted state and content of a clip.
    /// </summary>
    public async Task UpdateClipAsync(ClipItem clip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Clips.Update(clip);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the total number of clips stored.
    /// </summary>
    public async Task<int> GetClipCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Clips.CountAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes all non-pinned clips.
    /// </summary>
    public async Task ClearHistoryAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var nonPinned = await context.Clips
            .Where(c => !c.IsPinned)
            .ToListAsync()
            .ConfigureAwait(false);

        context.Clips.RemoveRange(nonPinned);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the total paste count across all clips.
    /// </summary>
    public async Task<int> GetTotalPasteCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Clips.SumAsync(c => c.PasteCount).ConfigureAwait(false);
    }

    private async Task EnforceHistoryLimitAsync(SnapClipDbContext context)
    {
        int count = await context.Clips.CountAsync().ConfigureAwait(false);
        if (count <= _maxHistorySize)
            return;

        int excess = count - _maxHistorySize;
        var toDelete = await context.Clips
            .Where(c => !c.IsPinned)
            .OrderBy(c => c.CapturedAt)
            .Take(excess)
            .ToListAsync()
            .ConfigureAwait(false);

        context.Clips.RemoveRange(toDelete);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
