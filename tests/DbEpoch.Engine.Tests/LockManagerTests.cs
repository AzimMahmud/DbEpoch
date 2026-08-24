using DbEpoch.Engine.InMemory;
using Xunit;

namespace DbEpoch.Engine.Tests;

public class LockManagerTests
{
    [Fact]
    public async Task Acquire_WhenFree_ReturnsTrue()
    {
        var lm = new InMemoryMigrationLockManager();
        var result = await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        Assert.True(result);
    }

    [Fact]
    public async Task Acquire_WhenAlreadyLocked_ReturnsFalse()
    {
        var lm = new InMemoryMigrationLockManager();
        await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        var result = await lm.AcquireAsync("dev", "migration:dev", "user2", 30);
        Assert.False(result);
    }

    [Fact]
    public async Task Release_ThenAcquire_ReturnsTrue()
    {
        var lm = new InMemoryMigrationLockManager();
        await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        await lm.ReleaseAsync("dev", "migration:dev", "user1");
        var result = await lm.AcquireAsync("dev", "migration:dev", "user2", 30);
        Assert.True(result);
    }

    [Fact]
    public async Task Release_ByDifferentUser_DoesNotRelease()
    {
        var lm = new InMemoryMigrationLockManager();
        await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        await lm.ReleaseAsync("dev", "migration:dev", "user2");
        var isActive = await lm.IsActiveAsync("dev", "migration:dev");
        Assert.True(isActive);
    }

    [Fact]
    public async Task Renew_ExtendsLock()
    {
        var lm = new InMemoryMigrationLockManager();
        await lm.AcquireAsync("dev", "migration:dev", "user1", 1);
        await Task.Delay(50);
        var renewed = await lm.RenewAsync("dev", "migration:dev", "user1", 30);
        Assert.True(renewed);
        var isActive = await lm.IsActiveAsync("dev", "migration:dev");
        Assert.True(isActive);
    }

    [Fact]
    public async Task Renew_ByDifferentUser_ReturnsFalse()
    {
        var lm = new InMemoryMigrationLockManager();
        await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        var result = await lm.RenewAsync("dev", "migration:dev", "user2", 30);
        Assert.False(result);
    }

    [Fact]
    public async Task IsActive_WhenNotLocked_ReturnsFalse()
    {
        var lm = new InMemoryMigrationLockManager();
        var result = await lm.IsActiveAsync("dev", "migration:dev");
        Assert.False(result);
    }

    [Fact]
    public async Task Acquire_DifferentEnvironments_BothSucceed()
    {
        var lm = new InMemoryMigrationLockManager();
        var r1 = await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        var r2 = await lm.AcquireAsync("qa", "migration:qa", "user2", 30);
        Assert.True(r1);
        Assert.True(r2);
    }

    [Fact]
    public async Task Renew_WithEmptyUser_ReturnsFalse()
    {
        var lm = new InMemoryMigrationLockManager();
        await lm.AcquireAsync("dev", "migration:dev", "user1", 30);
        var result = await lm.RenewAsync("dev", "migration:dev", "", 30);
        Assert.False(result);
    }
}
