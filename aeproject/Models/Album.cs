using System;
using System.Collections.Generic;

namespace aeproject.Models;

public partial class Album
{
    public int AlbumId { get; set; }

    public string AlbumName { get; set; } = null!;

    public DateOnly? ReleaseDate { get; set; }

    public string? AlbumType { get; set; }

    public string? AlbumCover { get; set; }

    public string? Agency { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
