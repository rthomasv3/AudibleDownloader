using System;
using System.Collections.Generic;

namespace AudibleDownloader.Models;

internal class LibraryResult
{
    public string Asin { get; set; }
    public string ImageUrl { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string FullTitle
    {
        get
        {
            return String.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title}: {Subtitle}";
        }
    }
    public List<string> Authors { get; set; }
    public string AuthorsDisplay
    {
        get
        {
            return $"By: {String.Join(", ", Authors ?? [])}";
        }
    }
    public List<string> Narrators { get; set; }
    public string NarratorsDisplay
    {
        get
        {
            return $"Narrated by: {String.Join(", ", Narrators ?? [])}";
        }
    }
    public string Description { get; set; }
    public string DescriptionClean
    {
        get
        {
            return Description?.Replace("<p>", "")?.Replace("</p>", "") ?? String.Empty;
        }
    }
    public List<string> AvailableCodecs { get; set; }
    public int RuntimeMinutes { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public bool IsDownloaded { get; set; }
    public bool IsMerged { get; set; }
    public string Directory { get; set; }
    public List<LibraryRelationship> Relationships { get; set; }
    public bool IsPlayable { get; set; }
    public List<SeriesResult> Series { get; set; }
}
