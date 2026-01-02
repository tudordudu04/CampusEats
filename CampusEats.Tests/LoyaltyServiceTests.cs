using Xunit;
using CampusEats.Api.Infrastructure.Loyalty;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class LoyaltyServiceTests
{
    [Fact]
    public async Task AwardPoints_Should_Create_Account_If_Missing()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var service = new LoyaltyService(db);
        var userId = Guid.NewGuid();

        // Apelăm pentru un user care nu are cont de loialitate încă
        await service.AwardPointsForOrder(userId, Guid.NewGuid(), 150m);

        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(la => la.UserId == userId);
        Assert.NotNull(account);
        Assert.Equal(15, account.Points); // 150 / 10 = 15 puncte
    }
}