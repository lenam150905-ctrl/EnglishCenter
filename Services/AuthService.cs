using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EnglishCenter.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
     ApplicationDbContext context,
     IConfiguration configuration,
     IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.UserName == dto.UserName);

            if (exists)
            {
                return false;
            }
            var emailExists = await _context.Users
    .AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
            {
                throw new ArgumentException(
                    "Email đã được sử dụng.");
            }
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<LoginResultDto?> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == dto.UserName);

            if (user == null)
            {
                return null;
            }

            var validPassword =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash);

            if (!validPassword)
            {
                return null;
            }

            // Password đúng → gửi OTP
            await SendLoginOtpAsync(user.UserName);

            // Chưa cấp JWT
            return new LoginResultDto
            {
                RequiresTwoFactor = true,
                UserName = user.UserName,
                Auth = null
            };
        }

        private string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.UserName),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var expireMinutes =
                int.Parse(
                    _configuration["Jwt:ExpireMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
        public async Task<bool> ResetPasswordAsync(
     ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ArgumentException(
                    "Mật khẩu mới không được để trống.");
            }

            if (dto.NewPassword.Length < 6)
            {
                throw new ArgumentException(
                    "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new ArgumentException(
                    "Mật khẩu xác nhận không khớp.");
            }

            // Tìm User bằng Email
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == dto.Email);

            if (user == null)
            {
                throw new ArgumentException(
                    "Email không tồn tại.");
            }

            // Lấy OTP đã xác minh
            var resetOtp = await _context.PasswordResetOtps
                .Where(o =>
                    o.UserId == user.Id &&
                    o.IsVerified)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (resetOtp == null)
            {
                throw new ArgumentException(
                    "Bạn chưa xác minh OTP.");
            }

            // Kiểm tra hết hạn
            if (resetOtp.ExpiredAt <= DateTime.Now)
            {
                throw new ArgumentException(
                    "Mã OTP đã hết hạn.");
            }

            // Kiểm tra OTP
            if (resetOtp.Otp != dto.Otp)
            {
                throw new ArgumentException(
                    "Mã OTP không chính xác.");
            }

            // Đổi mật khẩu
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.NewPassword);

            // OTP chỉ dùng một lần
            resetOtp.IsVerified = false;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<string> ForgotPasswordAsync(
      ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            // Kiểm tra email
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == dto.Email);

            if (user == null)
            {
                throw new ArgumentException(
                    "Email không tồn tại.");
            }

            // Kiểm tra email
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentException(
                    "Tài khoản chưa có email.");
            }
            var lastOtp = await _context.PasswordResetOtps
    .Where(o => o.UserId == user.Id)
    .OrderByDescending(o => o.Id)
    .FirstOrDefaultAsync();
            if (lastOtp != null)
            {
                var seconds =
                    (DateTime.Now - lastOtp.CreatedAt).TotalSeconds;

                if (seconds < 60)
                {
                    var remaining = 60 - (int)seconds;

                    throw new ArgumentException(
                        $"Vui lòng chờ {remaining} giây trước khi gửi OTP mới.");
                }
            }
            var oldOtps = await _context.PasswordResetOtps
    .Where(o =>
        o.UserId == user.Id &&
        !o.IsVerified)
    .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.IsVerified = true;
            }
            // OTP 6 số
            var otp = Random.Shared
                .Next(100000, 1000000)
                .ToString();

            var resetOtp = new PasswordResetOtp
            {
                UserId = user.Id,
                Otp = otp,
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsVerified = false,
                FailedAttempts = 0
            };
            _context.PasswordResetOtps.Add(resetOtp);

            await _context.SaveChangesAsync();

            // Gửi OTP qua Email
            await _emailService.SendEmailAsync(
                user.Email,
                "Mã OTP đặt lại mật khẩu - English Center",
                $"Xin chào {user.UserName},\n\n" +
                $"Mã OTP đặt lại mật khẩu của bạn là: {otp}\n\n" +
                "Mã OTP có hiệu lực trong 5 phút.\n" +
                "Bạn chỉ được nhập sai tối đa 5 lần.\n\n" +
                "Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.");

            return otp;
        }
        public async Task<bool> VerifyOtpAsync(
     VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Otp))
            {
                throw new ArgumentException(
                    "Mã OTP không được để trống.");
            }

            if (dto.Otp.Length != 6 ||
                !dto.Otp.All(char.IsDigit))
            {
                throw new ArgumentException(
                    "Mã OTP phải gồm 6 chữ số.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == dto.Email);

            if (user == null)
            {
                throw new ArgumentException(
                    "Email không tồn tại.");
            }

            var resetOtp = await _context.PasswordResetOtps
                .Where(o =>
                    o.UserId == user.Id &&
                    !o.IsVerified)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (resetOtp == null)
            {
                throw new ArgumentException(
                    "Không tìm thấy mã OTP.");
            }

            // HẾT HẠN
            if (resetOtp.ExpiredAt <= DateTime.Now)
            {
                throw new ArgumentException(
                    "Mã OTP đã hết hạn.");
            }

            // GIỚI HẠN 5 LẦN
            if (resetOtp.FailedAttempts >= 5)
            {
                throw new ArgumentException(
                    "Bạn đã nhập sai OTP quá 5 lần.");
            }

            // SAI OTP
            if (resetOtp.Otp != dto.Otp)
            {
                resetOtp.FailedAttempts++;

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    $"Mã OTP không chính xác. " +
                    $"Bạn còn {5 - resetOtp.FailedAttempts} lần thử.");
            }

            // ĐÚNG OTP
            resetOtp.IsVerified = true;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> SendLoginOtpAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                throw new ArgumentException(
                    "Tài khoản không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentException(
                    "Tài khoản chưa có email.");
            }

            // =========================
            // KIỂM TRA 60 GIÂY
            // =========================

            var lastOtp = await _context.LoginOtps
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (lastOtp != null)
            {
                var seconds =
                    (DateTime.Now - lastOtp.CreatedAt).TotalSeconds;

                if (seconds < 60)
                {
                    var remaining =
                        60 - (int)seconds;

                    throw new ArgumentException(
                        $"Vui lòng chờ {remaining} giây trước khi gửi OTP mới.");
                }
            }

            // =========================
            // VÔ HIỆU OTP CŨ
            // =========================

            var oldOtps = await _context.LoginOtps
                .Where(o =>
                    o.UserId == user.Id &&
                    !o.IsVerified)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.IsVerified = true;
            }

            // =========================
            // TẠO OTP MỚI
            // =========================

            var otp = Random.Shared
                .Next(100000, 1000000)
                .ToString();

            var loginOtp = new LoginOtp
            {
                UserId = user.Id,
                Otp = otp,
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsVerified = false,
                FailedAttempts = 0
            };

            _context.LoginOtps.Add(loginOtp);

            await _context.SaveChangesAsync();

            // =========================
            // GỬI EMAIL
            // =========================

            await _emailService.SendEmailAsync(
                user.Email,
                "Mã xác minh đăng nhập - English Center",
                $"Xin chào {user.UserName},\n\n" +
                $"Mã OTP đăng nhập của bạn là: {otp}\n\n" +
                "Mã có hiệu lực trong 5 phút.\n" +
                "Bạn được nhập sai tối đa 5 lần.\n\n" +
                "Nếu bạn không thực hiện đăng nhập, hãy bỏ qua email.");

            return true;
        }
        public async Task<LoginResultDto?> VerifyLoginOtpAsync(
    VerifyLoginOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Otp))
            {
                throw new ArgumentException(
                    "Mã OTP không được để trống.");
            }

            // OTP phải 6 số
            if (dto.Otp.Length != 6 ||
                !dto.Otp.All(char.IsDigit))
            {
                throw new ArgumentException(
                    "Mã OTP phải gồm 6 chữ số.");
            }

            // Tìm User
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == dto.UserName);

            if (user == null)
            {
                throw new ArgumentException(
                    "Tài khoản không tồn tại.");
            }

            // OTP mới nhất chưa sử dụng
            var loginOtp = await _context.LoginOtps
                .Where(o =>
                    o.UserId == user.Id &&
                    !o.IsVerified)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (loginOtp == null)
            {
                throw new ArgumentException(
                    "Không tìm thấy mã OTP đăng nhập.");
            }

            // =========================
            // HẾT HẠN 5 PHÚT
            // =========================

            if (loginOtp.ExpiredAt <= DateTime.Now)
            {
                // Vô hiệu hóa OTP hết hạn
                loginOtp.IsVerified = true;

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    "Mã OTP đã hết hạn.");
            }

            // =========================
            // TỐI ĐA 5 LẦN
            // =========================

            if (loginOtp.FailedAttempts >= 5)
            {
                loginOtp.IsVerified = true;

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    "Bạn đã nhập sai OTP quá 5 lần.");
            }

            // =========================
            // KIỂM TRA OTP
            // =========================

            if (loginOtp.Otp != dto.Otp)
            {
                loginOtp.FailedAttempts++;

                // Sai lần thứ 5 → vô hiệu OTP
                if (loginOtp.FailedAttempts >= 5)
                {
                    loginOtp.IsVerified = true;

                    await _context.SaveChangesAsync();

                    throw new ArgumentException(
                        "Bạn đã nhập sai OTP quá 5 lần. Mã OTP đã bị vô hiệu hóa.");
                }

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    $"Mã OTP không chính xác. " +
                    $"Bạn còn {5 - loginOtp.FailedAttempts} lần thử.");
            }

            // =========================
            // OTP ĐÚNG
            // =========================

            loginOtp.IsVerified = true;

            await _context.SaveChangesAsync();

            // =========================
            // TẠO JWT
            // =========================

            var token = GenerateToken(user);

            var auth = new AuthResponseDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = user.Role,
                Token = token
            };

            return new LoginResultDto
            {
                RequiresTwoFactor = false,
                UserName = user.UserName,
                Auth = auth
            };
        }
    }
}