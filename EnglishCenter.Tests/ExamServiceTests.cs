using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EnglishCenter.Tests;

public sealed class ExamServiceTests
{
    private readonly Mock<IExamRepository> _exams = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IGradeRepository> _grades = new();
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<ICertificateRepository> _certificates = new();

    private ExamService CreateService() => new(
        _exams.Object,
        _courses.Object,
        _audit.Object,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        _notifications.Object,
        _grades.Object,
        _students.Object,
        _enrollments.Object,
        _invoices.Object,
        _certificates.Object);

    [Fact]
    public async Task CanStartExamAsync_WhenStudentHasCertificate_ReturnsDetailedCompletionMessage()
    {
        int studentId = 1;
        int examId = 10;
        int courseId = 5;

        _exams.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exam { Id = examId, CourseId = courseId, ExamName = "Final Exam" });
        _students.Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = studentId, FullName = "Nguyen Van A" });
        _enrollmentRepositorySetup(studentId, courseId);

        _grades.Setup(x => x.GetAllAsync(null, examId, studentId, null, null, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Grade> { new() { Id = 1, ExamId = examId, StudentId = studentId, Score = 9.0m } }, 1));

        _certificates.Setup(x => x.GetAllAsync(null, studentId, courseId, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Certificate> { new() { Id = 1, CertificateCode = "CERT-2026-001", StudentId = studentId, CourseId = courseId } }, 1));

        var result = await CreateService().CanStartExamAsync(studentId, examId);

        Assert.False(result.CanStart);
        Assert.True(result.HasAttempted);
        Assert.True(result.HasCertificate);
        Assert.Equal("CERT-2026-001", result.CertificateCode);
        Assert.Equal(9.0m, result.Score);
        Assert.Contains("Bạn đã hoàn thành bài thi và được cấp chứng chỉ", result.Message);
        Assert.Contains("CERT-2026-001", result.Message);
    }

    [Fact]
    public async Task CanStartExamAsync_WhenStudentHasGradeButNoCertificate_ReturnsCompletedExamMessage()
    {
        int studentId = 1;
        int examId = 10;
        int courseId = 5;

        _exams.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exam { Id = examId, CourseId = courseId, ExamName = "Final Exam" });
        _students.Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = studentId });
        _enrollmentRepositorySetup(studentId, courseId);

        _grades.Setup(x => x.GetAllAsync(null, examId, studentId, null, null, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Grade> { new() { Id = 1, ExamId = examId, StudentId = studentId, Score = 7.5m } }, 1));

        _certificates.Setup(x => x.GetAllAsync(null, studentId, courseId, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Certificate>(), 0));

        var result = await CreateService().CanStartExamAsync(studentId, examId);

        Assert.False(result.CanStart);
        Assert.True(result.HasAttempted);
        Assert.False(result.HasCertificate);
        Assert.Equal(7.5m, result.Score);
        Assert.Contains("Bạn đã hoàn thành bài thi này", result.Message);
    }

    [Fact]
    public async Task CanStartExamAsync_WhenTuitionNotPaid_ReturnsUnpaidMessage()
    {
        int studentId = 1;
        int examId = 10;
        int courseId = 5;

        _exams.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exam { Id = examId, CourseId = courseId, ExamName = "Final Exam" });
        _students.Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = studentId });
        _enrollmentRepositorySetup(studentId, courseId);

        _grades.Setup(x => x.GetAllAsync(null, examId, studentId, null, null, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Grade>(), 0));
        _grades.Setup(x => x.GetByStudentAndCourseAsync(studentId, courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Grade?)null);

        _certificates.Setup(x => x.GetAllAsync(null, studentId, courseId, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Certificate>(), 0));

        _invoices.Setup(x => x.HasPaidInvoiceForStudentAndCourseAsync(studentId, courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().CanStartExamAsync(studentId, examId);

        Assert.False(result.CanStart);
        Assert.False(result.HasAttempted);
        Assert.Contains("Bạn chưa hoàn tất đóng học phí", result.Message);
    }

    [Fact]
    public async Task CanStartExamAsync_WhenEligibleAndPaid_ReturnsCanStartTrue()
    {
        int studentId = 1;
        int examId = 10;
        int courseId = 5;

        _exams.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exam { Id = examId, CourseId = courseId, ExamName = "Final Exam" });
        _students.Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = studentId });
        _enrollmentRepositorySetup(studentId, courseId);

        _grades.Setup(x => x.GetAllAsync(null, examId, studentId, null, null, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Grade>(), 0));
        _grades.Setup(x => x.GetByStudentAndCourseAsync(studentId, courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Grade?)null);

        _certificates.Setup(x => x.GetAllAsync(null, studentId, courseId, null, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Certificate>(), 0));

        _invoices.Setup(x => x.HasPaidInvoiceForStudentAndCourseAsync(studentId, courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().CanStartExamAsync(studentId, examId);

        Assert.True(result.CanStart);
        Assert.False(result.HasAttempted);
        Assert.Equal("Bạn đủ điều kiện bắt đầu bài kiểm tra.", result.Message);
    }

    private void _enrollmentRepositorySetup(int studentId, int courseId)
    {
        _enrollments.Setup(x => x.GetByStudentAndCourseAsync(studentId, courseId))
            .ReturnsAsync(new Enrollment { Id = 100, StudentId = studentId, CourseId = courseId, Status = "Active" });
    }
}
