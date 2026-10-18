using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EnglishCenter.Tests;

public sealed class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<ITeacherRepository> _teachers = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<INotificationService> _notifications = new();

    private StudentService CreateService() => new(
        _students.Object,
        _teachers.Object,
        _users.Object,
        _audit.Object,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        _notifications.Object);

    [Fact]
    public async Task UpdateAsync_WhenReassigningUserId_ClearsFromOtherStudentsAndTeachers()
    {
        int studentId = 2;
        int targetUserId = 10;

        var existingStudent = new Student
        {
            Id = studentId,
            FullName = "Nguyen Van B",
            DateOfBirth = DateTime.Now.AddYears(-20),
            Email = "studentB@test.com",
            Phone = "0912345678",
            Address = "HN",
            UserId = null
        };

        var targetUser = new User
        {
            Id = targetUserId,
            UserName = "user10",
            Email = "user10@test.com",
            Role = "Student"
        };

        _students.Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(existingStudent);
        _students.Setup(x => x.ExistsByEmailAsync(existingStudent.Email, studentId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _students.Setup(x => x.ExistsByPhoneAsync(existingStudent.Phone, studentId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _students.Setup(x => x.UpdateAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _students.Setup(x => x.ClearUserIdAsync(targetUserId, studentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachers.Setup(x => x.ClearUserIdAsync(targetUserId, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _users.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);

        var updateDto = new StudentUpdateDto
        {
            FullName = "Nguyen Van B",
            DateOfBirth = DateTime.Now.AddYears(-20),
            Email = "studentB@test.com",
            Phone = "0912345678",
            Address = "HN",
            UserId = targetUserId
        };

        var result = await CreateService().UpdateAsync(studentId, updateDto);

        Assert.True(result);
        _students.Verify(x => x.ClearUserIdAsync(targetUserId, studentId, It.IsAny<CancellationToken>()), Times.Once);
        _teachers.Verify(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenReassigningUserId_ClearsFromOtherStudentsAndTeachers()
    {
        int targetUserId = 15;

        var targetUser = new User
        {
            Id = targetUserId,
            UserName = "user15",
            Email = "user15@test.com",
            Role = "Teacher" // Was Teacher, will be set to Student
        };

        _students.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _students.Setup(x => x.ExistsByPhoneAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _students.Setup(x => x.CreateAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _students.Setup(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachers.Setup(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _users.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);
        _users.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var createDto = new StudentCreateDto
        {
            FullName = "Nguyen Van C",
            DateOfBirth = DateTime.Now.AddYears(-20),
            Email = "studentC@test.com",
            Phone = "0987654321",
            Address = "HCM",
            UserId = targetUserId
        };

        var result = await CreateService().CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal("Student", targetUser.Role);
        _students.Verify(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
        _teachers.Verify(x => x.ClearUserIdAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUserId && u.Role == "Student"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
