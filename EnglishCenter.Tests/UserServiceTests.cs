using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EnglishCenter.Tests;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<ITeacherRepository> _teachers = new();

    private UserService CreateService() => new(
        _users.Object,
        _audit.Object,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        _notifications.Object,
        _students.Object,
        _teachers.Object);

    [Fact]
    public async Task UpdateAsync_WhenRoleChangedFromTeacherToStudent_ClearsTeacherUserId()
    {
        int userId = 42;
        var existingUser = new User
        {
            Id = userId,
            UserName = "teacher1",
            Email = "teacher1@test.com",
            Role = "Teacher"
        };

        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _users.Setup(x => x.ExistsByUserNameAsync("teacher1", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.ExistsByEmailAsync("teacher1@test.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var updateDto = new UserUpdateDto
        {
            UserName = "teacher1",
            Email = "teacher1@test.com",
            Role = "Student"
        };

        var result = await CreateService().UpdateAsync(userId, updateDto);

        Assert.True(result);
        _teachers.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _students.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleChangedFromStudentToTeacher_ClearsStudentUserId()
    {
        int userId = 99;
        var existingUser = new User
        {
            Id = userId,
            UserName = "student1",
            Email = "student1@test.com",
            Role = "Student"
        };

        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _users.Setup(x => x.ExistsByUserNameAsync("student1", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.ExistsByEmailAsync("student1@test.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var updateDto = new UserUpdateDto
        {
            UserName = "student1",
            Email = "student1@test.com",
            Role = "Teacher"
        };

        var result = await CreateService().UpdateAsync(userId, updateDto);

        Assert.True(result);
        _students.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _teachers.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleChangedToAdmin_ClearsBothStudentAndTeacherUserId()
    {
        int userId = 10;
        var existingUser = new User
        {
            Id = userId,
            UserName = "user10",
            Email = "user10@test.com",
            Role = "Teacher"
        };

        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _users.Setup(x => x.ExistsByUserNameAsync("user10", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.ExistsByEmailAsync("user10@test.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var updateDto = new UserUpdateDto
        {
            UserName = "user10",
            Email = "user10@test.com",
            Role = "Admin"
        };

        var result = await CreateService().UpdateAsync(userId, updateDto);

        Assert.True(result);
        _teachers.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _students.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleNotChanged_DoesNotClearUserId()
    {
        int userId = 10;
        var existingUser = new User
        {
            Id = userId,
            UserName = "teacher10",
            Email = "teacher10@test.com",
            Role = "Teacher"
        };

        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _users.Setup(x => x.ExistsByUserNameAsync("teacher10", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.ExistsByEmailAsync("teacher10@test.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var updateDto = new UserUpdateDto
        {
            UserName = "teacher10",
            Email = "teacher10@test.com",
            Role = "Teacher"
        };

        var result = await CreateService().UpdateAsync(userId, updateDto);

        Assert.True(result);
        _teachers.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
        _students.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ClearsBothStudentAndTeacherUserId()
    {
        int userId = 5;
        var existingUser = new User
        {
            Id = userId,
            UserName = "user5",
            Email = "user5@test.com",
            Role = "Teacher"
        };

        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _users.Setup(x => x.SoftDeleteAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().DeleteAsync(userId);

        Assert.True(result);
        _teachers.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _students.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
