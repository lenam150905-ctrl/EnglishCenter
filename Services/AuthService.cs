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
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            // 1. Kiểm tra UserName
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            // 2. Kiểm tra mật khẩu mới
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ArgumentException(
                    "Mật khẩu mới không được để trống.");
            }

            // 3. Kiểm tra độ dài
            if (dto.NewPassword.Length < 6)
            {
                throw new ArgumentException(
                    "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            // 4. Kiểm tra xác nhận mật khẩu
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new ArgumentException(
                    "Mật khẩu xác nhận không khớp.");
            }

            // 5. Tìm tài khoản
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == dto.UserName);

            if (user == null)
            {
                throw new ArgumentException(
                    "Tài khoản không tồn tại.");
            }
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

            if (resetOtp.ExpiredAt <= DateTime.Now)
            {
                throw new ArgumentException(
                    "Mã OTP đã hết hạn.");
            }

            if (resetOtp.Otp != dto.Otp)
            {
                throw new ArgumentException(
                    "Mã OTP không chính xác.");
            }
            // 6. Đổi mật khẩu
            user.PasswordHash =
      BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            resetOtp.IsVerified = false;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<string> ForgotPasswordAsync(
    ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == dto.UserName);

            if (user == null)
            {
                throw new ArgumentException(
                    "Tài khoản không tồn tại.");
            }

            // OTP 6 số
            var otp = Random.Shared
                .Next(100000, 1000000)
                .ToString();

            var resetOtp = new PasswordResetOtp
            {
                UserId = user.Id,
                Otp = otp,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsVerified = false,
                FailedAttempts = 0
            };

            _context.PasswordResetOtps.Add(resetOtp);

            await _context.SaveChangesAsync();

            return otp;
        }
        public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto)
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

            if (dto.Otp.Length != 6 ||
                !dto.Otp.All(char.IsDigit))
            {
                throw new ArgumentException(
                    "Mã OTP phải gồm 6 chữ số.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == dto.UserName);

            if (user == null)
            {
                throw new ArgumentException(
                    "Tài khoản không tồn tại.");
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

            if (resetOtp.ExpiredAt < DateTime.Now)
            {
                throw new ArgumentException(
                    "Mã OTP đã hết hạn.");
            }

            if (resetOtp.FailedAttempts >= 5)
            {
                throw new ArgumentException(
                    "Bạn đã nhập sai OTP quá 5 lần.");
            }

            if (resetOtp.Otp != dto.Otp)
            {
                resetOtp.FailedAttempts++;

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    $"Mã OTP không chính xác. Bạn còn {5 - resetOtp.FailedAttempts} lần thử.");
            }

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

            var otp = Random.Shared
                .Next(100000, 1000000)
                .ToString();

            var loginOtp = new LoginOtp
            {
                UserId = user.Id,
                Otp = otp,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsVerified = false,
                FailedAttempts = 0
            };

            _context.LoginOtps.Add(loginOtp);

            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(
                user.Email,
                "Mã xác minh đăng nhập - English Center",
                $"Mã OTP của bạn là: {otp}\n\n" +
                "Mã có hiệu lực trong 5 phút.");

            return true;
        }
        public async Task<LoginResultDto?> VerifyLoginOtpAsync(
       VerifyLoginOtpDto dto)
        {
            // 1. Kiểm tra UserName
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            // 2. Kiểm tra OTP
            if (string.IsNullOrWhiteSpace(dto.Otp))
            {
                throw new ArgumentException(
                    "Mã OTP không được để trống.");
            }

            // 3. OTP phải có 6 chữ số
            if (dto.Otp.Length != 6 ||
                !dto.Otp.All(char.IsDigit))
            {
                throw new ArgumentException(
                    "Mã OTP phải gồm 6 chữ số.");
            }

            // 4. Tìm User
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == dto.UserName);

            if (user == null)
            {
                throw new ArgumentException(
                    "Tài khoản không tồn tại.");
            }

            // 5. Lấy OTP mới nhất
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

            // 6. Kiểm tra hết hạn
            if (loginOtp.ExpiredAt <= DateTime.Now)
            {
                throw new ArgumentException(
                    "Mã OTP đã hết hạn.");
            }

            // 7. Giới hạn số lần sai
            if (loginOtp.FailedAttempts >= 5)
            {
                throw new ArgumentException(
                    "Bạn đã nhập sai OTP quá 5 lần.");
            }

            // 8. Kiểm tra OTP
            if (loginOtp.Otp != dto.Otp)
            {
                loginOtp.FailedAttempts++;

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    $"Mã OTP không chính xác. " +
                    $"Bạn còn {5 - loginOtp.FailedAttempts} lần thử.");
            }

            // 9. OTP đúng
            loginOtp.IsVerified = true;

            await _context.SaveChangesAsync();

            // 10. Tạo JWT
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