using System;
using System.Collections.Generic;

namespace aeproject.Models;

public partial class Song
{
    public int SongId { get; set; }

    public string SongName { get; set; } = null!;

    public TimeOnly? Duration { get; set; }

    public string? Language { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public int? AlbumId { get; set; }

    public string? AudioFile { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Album? Album { get; set; }
}
