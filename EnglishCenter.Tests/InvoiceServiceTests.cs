using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EnglishCenter.Tests;

public sealed class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<INotificationService> _notifications = new();
    private InvoiceService Service() => new(_invoices.Object, _students.Object, _enrollments.Object, _audit.Object, _notifications.Object, new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

    [Fact]
    public async Task CreateAsync_Rejects_UnknownStudent()
    {
        _students.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Student?)null);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new() { StudentId = 10, Amount = 100000, InvoiceDate = DateTime.Now, Status = "Unpaid" }));
        Assert.Equal("Student không tồn tại.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_Rejects_ZeroAmount()
    {
        _students.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Student { Id = 10 });
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new() { StudentId = 10, Amount = 0, InvoiceDate = DateTime.Now, Status = "Unpaid" }));
        Assert.Equal("Số tiền phải lớn hơn 0.", error.Message);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    public async Task CreateAsync_Rejects_InvalidStatus(string status)
    {
        _students.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Student { Id = 10 });
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new() { StudentId = 10, Amount = 100000, InvoiceDate = DateTime.Now, Status = status }));
        Assert.Equal("Status không hợp lệ.", error.Message);
    }
}
