using Microsoft.EntityFrameworkCore;
using NotesApp.Data;

namespace NotesApp.Services
{
    public class NoteService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public NoteService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<List<Note>> GetAllAsync(int pageNumber, int pageSize)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await dbContext.Notes
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
        }

        public async Task<List<Note>> SearchAsync(string term)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notes = await dbContext.Notes
                    .ToListAsync();

                return notes
                    .Where(note => note.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || note.Text.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }
        }

        public async Task<int> GetTotalNotesCountAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await dbContext.Notes.CountAsync();
            }
        }

        public async Task<Note?> GetByIdAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await dbContext.Notes.FindAsync(id);
            }
        }

        public async Task CreateAsync(Note note)
        {
            if (string.IsNullOrEmpty(note.Title) || string.IsNullOrEmpty(note.Text))
            {
                throw new ArgumentException("Note Title and Text cannot be empty.");
            }

            try
            {
                var timestamp = DateTime.UtcNow;
                note.CreatedAt = timestamp;
                note.UpdatedAt = timestamp;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Notes.AddAsync(note);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while creating note: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(Note note)
        {
            if (string.IsNullOrEmpty(note.Title) || string.IsNullOrEmpty(note.Text))
            {
                throw new ArgumentException("Note Title and Text cannot be empty.");
            }

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var existingNote = await dbContext.Notes.FindAsync(note.ID);
                    if (existingNote != null)
                    {
                        existingNote.Title = note.Title;
                        existingNote.Text = note.Text;
                        existingNote.UpdatedAt = DateTime.UtcNow;
                    }
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while updating note: {ex.Message}");
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var note = await dbContext.Notes.FindAsync(id);
                    if (note != null)
                    {
                        dbContext.Notes.Remove(note);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while deleting note: {ex.Message}");
            }
        }
    }
}