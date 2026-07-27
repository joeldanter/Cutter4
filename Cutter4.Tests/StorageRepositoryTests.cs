using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cutter4.Domain;
using Cutter4.Persistence.Repositories;

namespace Cutter4.Tests
{
    public class StorageRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        DatabaseFixture fixture;
        IStorageRepository repository;

        public StorageRepositoryTests(DatabaseFixture fixture)
        {
            this.fixture = fixture;
            repository = new StorageRepository(fixture.ContextFactory, new WeakReferenceMessenger());
        }

        public async Task InitializeAsync()
        {
            await fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task OnEmpty()
        {
            IDictionary<PanelKey, int> storage = await repository.StoragePanelsAsync();
            IList<StorageEdit> edits = await repository.ListStorageEditsAsync();

            Assert.Empty(edits);
            Assert.Empty(storage);
        }

        public static IEnumerable<object[]> EditStorageData()
        {
            yield return new object[] { new StorageEdit(DateTime.Now, "aa", new Dictionary<PanelKey, int>() {
                { new(PanelColors.F100, PanelWidths.Width500mm, PanelKey.MaxLength), 1} }) };
            yield return new object[] { new StorageEdit(DateTime.Now, "", new Dictionary<PanelKey, int>()) };

            yield return new object[] { new StorageEdit(DateTime.Now, "", new Dictionary<PanelKey, int>() {
                { new(PanelColors.F100, PanelWidths.Width500mm, PanelKey.MaxLength), 3 },
                { new(PanelColors.F201, PanelWidths.Width500mm, PanelKey.MaxLength), 1 }
            }) };

            yield return new object[] { new StorageEdit(DateTime.Now, "", new Dictionary<PanelKey, int>() {
                { new(PanelColors.F100, PanelWidths.Width500mm, PanelKey.MaxLength), 20 },
                { new(PanelColors.F201, PanelWidths.Width500mm, PanelKey.MaxLength), 3 }
            }) };

            yield return new object[] { new StorageEdit(DateTime.Now, "", new Dictionary<PanelKey, int>() {
                { new(PanelColors.F100, PanelWidths.Width500mm, PanelKey.MaxLength), 3 },
                { new(PanelColors.F201, PanelWidths.Width500mm, PanelKey.MaxLength), 7 },
                { new(PanelColors.F201, PanelWidths.Width610mm, PanelKey.MaxLength), 4 }
            }) };

            yield return new object[] { new StorageEdit(DateTime.Now, "", new Dictionary<PanelKey, int>() {
                { new(PanelColors.F100, PanelWidths.Width500mm, PanelKey.MaxLength), 4 },
                { new(PanelColors.F201, PanelWidths.Width500mm, 1), 3 },
                { new(PanelColors.F201, PanelWidths.Width500mm, PanelKey.MaxLength), 1 },
                { new(PanelColors.F201, PanelWidths.Width610mm, PanelKey.MaxLength), 10000 }
            }) };
        }

        [Theory]
        [MemberData(nameof(EditStorageData))]
        public async Task EditStorageTest(StorageEdit edit)
        {
            IDictionary<PanelKey, int> expected = edit.StorageEditItems;
            
            await repository.EditStorageAsync(edit);

            IList<StorageEdit> edits = await repository.ListStorageEditsAsync();
            Assert.Single(edits);
            StorageEdit savedEdit = edits[0];
            Assert.Equal(savedEdit.Message, edit.Message);
            double msDiff = Math.Abs((savedEdit.Date - edit.Date).TotalMilliseconds);
            Assert.True(msDiff <= 1);
            Assert.Equal(savedEdit.StorageEditItems.Count, edit.StorageEditItems.Count);
            Assert.True(savedEdit.StorageEditItems
                .Zip(edit.StorageEditItems)
                .All(p => p.First.Key == p.Second.Key
                       && p.First.Value == p.Second.Value));

            IDictionary<PanelKey, int> storage = await repository.StoragePanelsAsync();
            Assert.True(storage.All(p => expected[p.Key] == p.Value));
        }
    }
}
