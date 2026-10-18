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

    private UserService CreateService(int? currentUserId = null)
    {
        var context = new DefaultHttpContext();
        if (currentUserId.HasValue)
        {
            context.Items["AuditUserId"] = currentUserId.Value;
        }
        return new(
            _users.Object,
            _audit.Object,
            new HttpContextAccessor { HttpContext = context },
            _notifications.Object,
            _students.Object,
            _teachers.Object);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminEditsAnotherAdmin_ThrowsArgumentException()
    {
        int currentAdminId = 1;
        int targetAdminId = 2;

        var targetAdmin = new User
        {
            Id = targetAdminId,
            UserName = "admin2",
            Email = "admin2@test.com",
            Role = "Admin"
        };

        _users.Setup(x => x.GetByIdAsync(targetAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(targetAdmin);

        var updateDto = new UserUpdateDto
        {
            UserName = "admin2_updated",
            Email = "admin2_updated@test.com",
            Role = "Admin"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(currentAdminId).UpdateAsync(targetAdminId, updateDto));

        Assert.Equal("Quản trị viên không được phép sửa tài khoản của quản trị viên khác.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminEditsOwnRole_ThrowsArgumentException()
    {
        int currentAdminId = 1;

        var currentAdmin = new User
        {
            Id = currentAdminId,
            UserName = "admin1",
            Email = "admin1@test.com",
            Role = "Admin"
        };

        _users.Setup(x => x.GetByIdAsync(currentAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(currentAdmin);

        var updateDto = new UserUpdateDto
        {
            UserName = "admin1",
            Email = "admin1@test.com",
            Role = "Teacher" // Attempting to change own role
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(currentAdminId).UpdateAsync(currentAdminId, updateDto));

        Assert.Equal("Quản trị viên không được phép tự sửa vai trò của chính mình.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminEditsOwnProfileKeepingRole_Succeeds()
    {
        int currentAdminId = 1;

        var currentAdmin = new User
        {
            Id = currentAdminId,
            UserName = "admin1",
            Email = "admin1@test.com",
            Role = "Admin"
        };

        _users.Setup(x => x.GetByIdAsync(currentAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(currentAdmin);
        _users.Setup(x => x.ExistsByUserNameAsync("admin1_new", currentAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.ExistsByEmailAsync("admin1_new@test.com", currentAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var updateDto = new UserUpdateDto
        {
            UserName = "admin1_new",
            Email = "admin1_new@test.com",
            Role = "Admin"
        };

        var result = await CreateService(currentAdminId).UpdateAsync(currentAdminId, updateDto);

        Assert.True(result);
        _users.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Id == currentAdminId && u.UserName == "admin1_new"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdminDeletesAnotherAdmin_ThrowsArgumentException()
    {
        int currentAdminId = 1;
        int targetAdminId = 2;

        var targetAdmin = new User
        {
            Id = targetAdminId,
            UserName = "admin2",
            Email = "admin2@test.com",
            Role = "Admin"
        };

        _users.Setup(x => x.GetByIdAsync(targetAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(targetAdmin);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(currentAdminId).DeleteAsync(targetAdminId));

        Assert.Equal("Quản trị viên không được phép xóa tài khoản của quản trị viên khác.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdminDeletesSelf_ThrowsArgumentException()
    {
        int currentAdminId = 1;

        var currentAdmin = new User
        {
            Id = currentAdminId,
            UserName = "admin1",
            Email = "admin1@test.com",
            Role = "Admin"
        };

        _users.Setup(x => x.GetByIdAsync(currentAdminId, It.IsAny<CancellationToken>())).ReturnsAsync(currentAdmin);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(currentAdminId).DeleteAsync(currentAdminId));

        Assert.Equal("Bạn không thể tự xóa tài khoản của chính mình.", ex.Message);
    }

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

        var result = await CreateService(1).UpdateAsync(userId, updateDto);

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

        var result = await CreateService(1).UpdateAsync(userId, updateDto);

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

        var result = await CreateService(1).UpdateAsync(userId, updateDto);

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

        var result = await CreateService(1).UpdateAsync(userId, updateDto);

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

        var result = await CreateService(1).DeleteAsync(userId);

        Assert.True(result);
        _teachers.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _students.Verify(x => x.ClearUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
