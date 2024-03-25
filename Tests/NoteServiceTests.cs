using Microsoft.EntityFrameworkCore;
using NotesApp.Data;
using NotesApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Tests
{
    [TestFixture]
    public class NoteServiceTests
    {
        private NoteService _noteService;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void SetUp()
        {
            var serviceCollection = new ServiceCollection();
            var ñonfiguration = new ConfigurationBuilder().Build();

            serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDB");
            });
            serviceCollection.AddSingleton<IConfiguration>(ñonfiguration);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            _noteService = new NoteService(serviceScopeFactory);

            _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task GetAllAsync_ReturnsCorrectNumberOfNotes()
        {
            // Arrange
            await _dbContext.Notes.AddRangeAsync(
                new Note { Title = "Note 1", Text = "Text 1", CreatedAt = DateTime.Now.AddDays(-2) },
                new Note { Title = "Note 2", Text = "Text 2", CreatedAt = DateTime.Now.AddDays(-1) },
                new Note { Title = "Note 3", Text = "Text 3", CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            int pageNumber = 1;
            int pageSize = 2;

            // Act
            var result = await _noteService.GetAllAsync(pageNumber, pageSize);

            // Assert
            Assert.AreEqual(pageSize, result.Count);
        }

        [Test]
        public async Task GetAllAsync_ReturnsCorrectNotesBasedOnPagination()
        {
            // Arrange
            await _dbContext.Notes.AddRangeAsync(
                new Note { Title = "Note 1", Text = "Text 1", CreatedAt = DateTime.Now.AddDays(-3) },
                new Note { Title = "Note 2", Text = "Text 2", CreatedAt = DateTime.Now.AddDays(-2) },
                new Note { Title = "Note 3", Text = "Text 3", CreatedAt = DateTime.Now.AddDays(-1) },
                new Note { Title = "Note 4", Text = "Text 4", CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            int pageNumber = 2;
            int pageSize = 2;

            // Act
            var result = await _noteService.GetAllAsync(pageNumber, pageSize);

            // Assert
            Assert.AreEqual(pageSize, result.Count);
            Assert.AreEqual("Note 2", result[0].Title);
            Assert.AreEqual("Note 1", result[1].Title);
        }

        [Test]
        public async Task SearchAsync_ReturnsCorrectNotes()
        {
            // Arrange
            await _dbContext.Notes.AddRangeAsync(
                new Note { Title = "Title 1", Text = "Text 1", CreatedAt = DateTime.Now },
                new Note { Title = "Title 2", Text = "Text 2", CreatedAt = DateTime.Now },
                new Note { Title = "Another", Text = "Text 3", CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            string searchTerm = "Title";

            // Act
            var result = await _noteService.SearchAsync(searchTerm);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(note => note.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task SearchAsync_ReturnsEmptyListIfNoMatches()
        {
            // Arrange
            await _dbContext.Notes.AddRangeAsync(
                new Note { Title = "Title 1", Text = "Text 1", CreatedAt = DateTime.Now },
                new Note { Title = "Title 2", Text = "Text 2", CreatedAt = DateTime.Now },
                new Note { Title = "Another Title", Text = "Text 3", CreatedAt = DateTime.Now }
            );
            await _dbContext.SaveChangesAsync();

            string searchTerm = "Nonexistent";

            // Act
            var result = await _noteService.SearchAsync(searchTerm);

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetTotalNotesCountAsync_ReturnsTotalNumberOfNotes()
        {
            // Arrange
            var notes = new List<Note>
            {
                new Note { Title = "Test Note 1", Text = "This is a test note 1." },
                new Note { Title = "Another Note", Text = "This is not a test note." },
                new Note { Title = "Test Note 2", Text = "This is another test note 2." }
            };

            await _dbContext.Notes.AddRangeAsync(notes);
            await _dbContext.SaveChangesAsync();

            // Act
            var totalCount = await _noteService.GetTotalNotesCountAsync();

            // Assert
            Assert.AreEqual(3, totalCount);
        }

        [Test]
        public async Task CreateAsync_AddsNoteToDatabase()
        {
            // Arrange
            var note = new Note { Title = "Test Note", Text = "This is a test note." };

            // Act
            await _noteService.CreateAsync(note);

            // Assert
            var savedNotes = await _dbContext.Notes.Where(n => n.Title == "Test Note").ToListAsync();
            Assert.AreEqual(1, savedNotes.Count);

            var savedNote = savedNotes.FirstOrDefault();
            Assert.AreEqual(note.Title, savedNote.Title);
            Assert.AreEqual(note.Text, savedNote.Text);
            Assert.AreEqual(note.CreatedAt, savedNote.CreatedAt);
            Assert.AreEqual(note.UpdatedAt, savedNote.UpdatedAt);
        }

        [Test]
        public async Task CreateAsync_DoesNotAddNoteWithoutTitleOrText()
        {
            // Arrange
            var noteWithoutTitle = new Note { Text = "This is a note without title." };
            var noteWithoutText = new Note { Title = "Note without text" };

            // Act and Assert
            try
            {
                await _noteService.CreateAsync(noteWithoutTitle);
                await _noteService.CreateAsync(noteWithoutText);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Expected ArgumentException was not thrown.");
        }


        [Test]
        public async Task UpdateAsync_UpdatesNoteInDatabase()
        {
            // Arrange
            var note = new Note { Title = "Initial Title", Text = "Initial Text" };
            await _dbContext.Notes.AddAsync(note);
            await _dbContext.SaveChangesAsync();

            var updatedNote = new Note { ID = note.ID, Title = "Updated Title", Text = "Updated Text" };

            // Act
            await _noteService.UpdateAsync(updatedNote);

            // Assert
            var savedNote = await _noteService.GetByIdAsync(note.ID);
            Assert.IsNotNull(savedNote);
            Assert.AreEqual(updatedNote.Title, savedNote.Title);
            Assert.AreEqual(updatedNote.Text, savedNote.Text);
            Assert.AreNotEqual(note.UpdatedAt, savedNote.UpdatedAt);
        }

        [Test]
        public async Task UpdateAsync_DoesNotUpdateNonexistentNote()
        {
            // Arrange
            var note = new Note { ID = 999, Title = "Nonexistent Title", Text = "Nonexistent Text" };

            // Act
            await _noteService.UpdateAsync(note);

            // Assert
            var savedNote = await _dbContext.Notes.FirstOrDefaultAsync(n => n.ID == note.ID);
            Assert.IsNull(savedNote);
        }

        [Test]
        public async Task UpdateAsync_DoesNotUpdateNoteWithoutTitleOrText()
        {
            // Arrange
            var note = new Note { Title = "Initial Title 1", Text = "Initial Text 1" };
            await _dbContext.Notes.AddAsync(note);
            await _dbContext.SaveChangesAsync();

            var noteWithoutTitle = new Note { ID = note.ID, Text = "This is a note without title." };
            var noteWithoutText = new Note { ID = note.ID, Title = "Note without text" };

            // Act and Assert
            try
            {
                await _noteService.UpdateAsync(noteWithoutTitle);
                await _noteService.UpdateAsync(noteWithoutText);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Expected ArgumentException was not thrown.");
        }
    }
}
