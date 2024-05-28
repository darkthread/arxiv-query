
using Microsoft.EntityFrameworkCore;

namespace PaperSearchService;

public class PaperMetadata
{
    public string Id { get; set; }
    public string Authors { get; set; }
    public string Title { get; set; }
    public string Categories { get; set; }
    public string Abstract { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("update_date")]
    public string UpdateDate { get; set; }
    public string AbstractCht { get; set; }
}

public class MyDbContext : DbContext
{
    public DbSet<PaperMetadata> PaperMetadata { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=arxiv.db");
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaperMetadata>()
            .HasIndex(p => p.Categories);
        modelBuilder.Entity<PaperMetadata>()
            .HasIndex(p => p.UpdateDate);
    }

    public void BatchInsert(IEnumerable<PaperMetadata> papers)
    {
        this.PaperMetadata.AddRange(papers);
        this.SaveChanges();
    }

    public IEnumerable<PaperMetadata> Search(string keywd, string st, string ed)
    {
        var papers = this.PaperMetadata
            .Where(p => string.IsNullOrEmpty(st) || p.UpdateDate.CompareTo(st) >= 0)
            .Where(p => string.IsNullOrEmpty(ed) || p.UpdateDate.CompareTo(ed) <= 0)
            .Where(p => p.Title.Contains(keywd) || p.Abstract.Contains(keywd))
            .OrderByDescending(p => p.UpdateDate);
        return papers;
    }
}