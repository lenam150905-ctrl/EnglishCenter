using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EnglishCenter.Tests;

public sealed class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<INotificationService> _notifications = new();
    private CourseService Service() => new(_courses.Object, _audit.Object, new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, _notifications.Object);

    [Fact]
    public async Task CreateAsync_Rejects_EmptyCourseName()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new() { CourseCode = "EC01", CourseName = " ", Duration = 10, TuitionFee = 1000 }));
        Assert.Equal("Tên khóa học không được để trống.", error.Message);
        _courses.Verify(x => x.CreateAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Rejects_NonPositiveDuration()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new() { CourseCode = "EC01", CourseName = "English A1", Duration = 0, TuitionFee = 1000 }));
        Assert.Equal("Thời lượng khóa học phải lớn hơn 0.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_Rejects_NegativeTuitionFee()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new() { CourseCode = "EC01", CourseName = "English A1", Duration = 10, TuitionFee = -1 }));
        Assert.Equal("Học phí không được âm.", error.Message);
    }

    [Fact]
    public async Task GetAllAsync_NormalizesPaging_AndMapsCourse()
    {
        _courses.Setup(x => x.GetAllAsync(null, null, null, null, false, 0, 0, It.IsAny<CancellationToken>())).ReturnsAsync((new List<Course> { new() { Id = 7, CourseCode = "EC07", CourseName = "IELTS Foundation", Description = "Starter", Duration = 24, TuitionFee = 3500000, Status = "Active" } }, 1));
        var result = await Service().GetAllAsync(null, null, null, null, false, 0, 0);
        Assert.Equal(1, result.Page); Assert.Equal(20, result.PageSize); Assert.Single(result.Data); Assert.Equal("IELTS Foundation", result.Data[0].CourseName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenCourseDoesNotExist()
    {
        _courses.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Course?)null);
        Assert.Null(await Service().GetByIdAsync(999));
    }
}
