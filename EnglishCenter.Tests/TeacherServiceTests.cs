using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EnglishCenter.Tests;

public sealed class TeacherServiceTests
{
    private readonly Mock<ITeacherRepository> _teachers = new();
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<INotificationService> _notifications = new();

    private TeacherService CreateService() => new(
        _teachers.Object,
        _students.Object,
        _users.Object,
        _audit.Object,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        _notifications.Object);

    [Fact]
    public async Task UpdateAsync_WhenReassigningUserId_ClearsFromOtherTeachersAndStudents()
    {
        int teacherId = 3;
        int targetUserId = 22;

        var existingTeacher = new Teacher
        {
            Id = teacherId,
            FullName = "Teacher John",
            Email = "john@test.com",
            Phone = "0933333333",
            Specialization = "IELTS",
            UserId = null
        };

        var targetUser = new User
        {
            Id = targetUserId,
            UserName = "user22",
            Email = "user22@test.com",
            Role = "Teacher"
        };

        _teachers.Setup(x => x.GetByIdAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTeacher);
        _teachers.Setup(x => x.ExistsByEmailAsync(existingTeacher.Email, teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _teachers.Setup(x => x.ExistsByPhoneAsync(existingTeacher.Phone, teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _teachers.Setup(x => x.UpdateAsync(It.IsAny<Teacher>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachers.Setup(x => x.ClearUserIdAsync(targetUserId, teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _students.Setup(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _users.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);

        var updateDto = new TeacherUpdateDto
        {
            FullName = "Teacher John",
            Email = "john@test.com",
            Phone = "0933333333",
            Specialization = "IELTS",
            UserId = targetUserId
        };

        var result = await CreateService().UpdateAsync(teacherId, updateDto);

        Assert.True(result);
        _teachers.Verify(x => x.ClearUserIdAsync(targetUserId, teacherId, It.IsAny<CancellationToken>()), Times.Once);
        _students.Verify(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenReassigningUserId_ClearsFromOtherTeachersAndStudents()
    {
        int targetUserId = 33;

        var targetUser = new User
        {
            Id = targetUserId,
            UserName = "user33",
            Email = "user33@test.com",
            Role = "Student" // Was Student, will be set to Teacher
        };

        _teachers.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _teachers.Setup(x => x.ExistsByPhoneAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _teachers.Setup(x => x.CreateAsync(It.IsAny<Teacher>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _teachers.Setup(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _students.Setup(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _users.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var createDto = new TeacherCreateDto
        {
            FullName = "Teacher Mary",
            Email = "mary@test.com",
            Phone = "0944444444",
            Specialization = "TOEIC",
            UserId = targetUserId
        };

        var result = await CreateService().CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal("Teacher", targetUser.Role);
        _teachers.Verify(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
        _students.Verify(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUserId && u.Role == "Teacher"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
