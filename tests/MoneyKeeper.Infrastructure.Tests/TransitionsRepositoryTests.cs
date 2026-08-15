using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class TransitionsRepositoryTests : RepositoryTestsBase
    {
        private TransitionsRepository _repository = null!;

        public TransitionsRepositoryTests(Common.DbFixture fixture) : base(fixture)
        {
            _repository = new TransitionsRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ValidTransition_TransitionIsSaved()
        {
            User user = await CreateTestUserAsync();
            Account sourceAccount = new Account { UserId = user.Id, Name = "Source", Balance = 500 };
            Account destinationAccount = new Account { UserId = user.Id, Name = "Destination", Balance = 100 };
            _context.Accounts.AddRange(sourceAccount, destinationAccount);
            await _context.SaveChangesAsync();

            Transition transition = new Transition
            {
                SourceAccountId = sourceAccount.Id,
                DestinationAccountId = destinationAccount.Id,
                Sum = 200,
                Description = "TestTransition",
                OldSourceAccountBalance = 500,
                NewSourceAccountBalance = 300,
                OldDestinationAccountBalance = 100,
                NewDestinationAccountBalance = 300
            };

            await _repository.AddAsync(transition, CancellationToken.None);

            Transition? fromDb = await _context.Transitions.FirstOrDefaultAsync(t => t.Description == "TestTransition");
            fromDb.Should().NotBeNull();
            fromDb!.SourceAccountId.Should().Be(sourceAccount.Id);
            fromDb.DestinationAccountId.Should().Be(destinationAccount.Id);
            fromDb.Sum.Should().Be(200);
            fromDb.OldSourceAccountBalance.Should().Be(500);
            fromDb.NewSourceAccountBalance.Should().Be(300);
            fromDb.OldDestinationAccountBalance.Should().Be(100);
            fromDb.NewDestinationAccountBalance.Should().Be(300);
            fromDb.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteAsync_ExistingTransition_TransitionIsDeleted()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "SrcDel", Balance = 0 };
            Account destination = new Account { UserId = user.Id, Name = "DstDel", Balance = 0 };
            Transition transition = new Transition
            {
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 50,
                OldSourceAccountBalance = 100,
                NewSourceAccountBalance = 50,
                OldDestinationAccountBalance = 0,
                NewDestinationAccountBalance = 50
            };
            transition.SourceAccount = source;
            transition.DestinationAccount = destination;
            _context.Accounts.AddRange(source, destination);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(transition.Id, CancellationToken.None);

            Transition? fromDb = await _context.Transitions.FirstOrDefaultAsync(t => t.Id == transition.Id);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingTransition_ReturnsTransitionWithIncludes()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "SrcGet" };
            Account destination = new Account { UserId = user.Id, Name = "DstGet" };
            Transition transition = new Transition
            {
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 100,
                OldSourceAccountBalance = 200,
                NewSourceAccountBalance = 100,
                OldDestinationAccountBalance = 50,
                NewDestinationAccountBalance = 150
            };
            transition.SourceAccount = source;
            transition.DestinationAccount = destination;
            _context.Accounts.AddRange(source, destination);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            Transition? result = await _repository.GetByIdAsync(transition.Id, CancellationToken.None);

            result.Should().NotBeNull();
            result!.SourceAccount.Should().NotBeNull();
            result.SourceAccount.Name.Should().Be("SrcGet");
            result.DestinationAccount.Should().NotBeNull();
            result.DestinationAccount.Name.Should().Be("DstGet");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentTransition_ReturnsNull()
        {
            Transition? result = await _repository.GetByIdAsync(99999, CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByUserId_ReturnsOnlyUserTransitions()
        {
            User user1 = await CreateTestUserAsync();
            User user2 = await CreateTestUserAsync(2);
            Account source1 = new Account { UserId = user1.Id, Name = "Src1" };
            Account destination1 = new Account { UserId = user1.Id, Name = "Dst1" };
            Account source2 = new Account { UserId = user2.Id, Name = "Src2" };
            Account destination2 = new Account { UserId = user2.Id, Name = "Dst2" };
            Transition transition1 = new Transition
            {
                SourceAccount = source1,
                DestinationAccount = destination1,
                SourceAccountId = source1.Id,
                DestinationAccountId = destination1.Id,
                Sum = 10
            };
            Transition transition2 = new Transition
            {
                SourceAccount = source2,
                DestinationAccount = destination2,
                SourceAccountId = source2.Id,
                DestinationAccountId = destination2.Id,
                Sum = 20
            };
            _context.Accounts.AddRange(source1, destination1, source2, destination2);
            _context.Transitions.AddRange(transition1, transition2);
            await _context.SaveChangesAsync();

            IQueryable<Transition> transitions = _repository.GetAllByUserId(user1.Id);   
            List<Transition> result = await transitions.ToListAsync();

            result.Should().HaveCount(1);
            result[0].SourceAccount.UserId.Should().Be(user1.Id);
        }

        [Fact]
        public async Task GetAllPagedAsync_ReturnsCorrectPage()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "PageSrc" };
            Account destination = new Account { UserId = user.Id, Name = "PageDst" };
            Transition transition1 = new Transition
            {
                SourceAccount = source,
                DestinationAccount = destination,
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 1
            };
            Transition transition2 = new Transition
            {
                SourceAccount = source,
                DestinationAccount = destination,
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 2
            };
            Transition transition3 = new Transition
            {
                SourceAccount = source,
                DestinationAccount = destination,
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 3
            };
            _context.Accounts.AddRange(source, destination);
            _context.Transitions.AddRange(transition1, transition2, transition3);
            await _context.SaveChangesAsync();

            IQueryable<Transition> query = _context.Transitions.Where(t => t.SourceAccount.UserId == user.Id);
            (List<Transition> items, int totalCount) = await _repository.GetAllPagedAsync(query, 2, 1, CancellationToken.None);

            items.Should().HaveCount(1);
            totalCount.Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync_ExistingTransition_UpdatesProperties()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "UpdSrc" };
            Account destination = new Account { UserId = user.Id, Name = "UpdDst" };
            Transition transition = new Transition
            {
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 100,
                Description = "Old",
                OldSourceAccountBalance = 200,
                NewSourceAccountBalance = 100,
                OldDestinationAccountBalance = 50,
                NewDestinationAccountBalance = 150
            };
            transition.SourceAccount = source;
            transition.DestinationAccount = destination;
            _context.Accounts.AddRange(source, destination);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            transition.Sum = 150;
            transition.Description = "New";
            transition.OldSourceAccountBalance = 250;
            transition.NewSourceAccountBalance = 100;
            transition.OldDestinationAccountBalance = 60;
            transition.NewDestinationAccountBalance = 210;
            Transition updated = await _repository.UpdateAsync(transition, CancellationToken.None);

            updated.Sum.Should().Be(150);
            updated.Description.Should().Be("New");
            updated.OldSourceAccountBalance.Should().Be(250);
            updated.NewSourceAccountBalance.Should().Be(100);
            updated.SourceAccount.Name.Should().Be("UpdSrc");
            updated.DestinationAccount.Name.Should().Be("UpdDst");
        }

        [Fact]
        public async Task AreAnySourceTransitionsAfterAsync_WhenTransitionExists_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "AfterSrc" };
            Account destination = new Account { UserId = user.Id, Name = "AfterDst" };
            Transition transition = new Transition
            {
                SourceAccount = source,
                DestinationAccount = destination,
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 50,
                Date = DateTime.Now.AddMinutes(10)
            };
            _context.Accounts.AddRange(source, destination);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnySourceTransitionsAfterAsync(source.Id, DateTime.Now, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreAnySourceTransitionsAfterAsync_NoTransitionAfterDate_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "NoAfterSrc" };
            Account destination = new Account { UserId = user.Id, Name = "NoAfterDst" };
            Transition transition = new Transition
            {
                SourceAccount = source,
                DestinationAccount = destination,
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                Sum = 50,
                Date = DateTime.Now.AddMinutes(-10)
            };
            _context.Accounts.AddRange(source, destination);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnySourceTransitionsAfterAsync(source.Id, DateTime.Now, CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}