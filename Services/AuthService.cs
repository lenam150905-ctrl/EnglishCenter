using EnglishCenter.API.DTOs;
using EnglishCenter.API.Jobs;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EnglishCenter.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobQueue _queue;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IBackgroundJobQueue queue)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _emailService = emailService;
            _queue = queue;
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            var exists = await _userRepository.ExistsByUserNameAsync(dto.UserName);
            if (exists)
            {
                return false;
            }

            var emailExists = await _userRepository.ExistsByEmailAsync(dto.Email);
            if (emailExists)
            {
                throw new ArgumentException("Email đã được sử dụng.");
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            await _userRepository.CreateAsync(user);
            return true;
        }

        public async Task<LoginResultDto?> LoginAsync(LoginDto dto)
        {
            var loginValue = dto.UserName.Trim();

            var user = await _userRepository.GetByUserNameOrEmailAsync(loginValue);
            if (user == null)
            {
                return null;
            }

            var validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!validPassword)
            {
                return null;
            }

            await CreateAndQueueLoginOtpAsync(user);

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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ArgumentException("Mật khẩu mới không được để trống.");
            }

            if (dto.NewPassword.Length < 6)
            {
                throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new ArgumentException("Mật khẩu xác nhận không khớp.");
            }

            var user = await _userRepository.GetByUserNameOrEmailAsync(dto.Email);
            if (user == null)
            {
                throw new ArgumentException("Email không tồn tại.");
            }

            var resetOtp = await _userRepository.GetLatestPasswordResetOtpAsync(user.Id, onlyVerified: true);
            if (resetOtp == null)
            {
                throw new ArgumentException("Bạn chưa xác minh OTP.");
            }

            if (resetOtp.ExpiredAt <= DateTime.Now)
            {
                throw new ArgumentException("Mã OTP đã hết hạn.");
            }

            if (resetOtp.Otp != dto.Otp)
            {
                throw new ArgumentException("Mã OTP không chính xác.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            resetOtp.IsVerified = false;
            await _userRepository.UpdatePasswordResetOtpAsync(resetOtp);

            return true;
        }

        public async Task<string> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email không được để trống.");
            }

            var user = await _userRepository.GetByUserNameOrEmailAsync(dto.Email);
            if (user == null)
            {
                throw new ArgumentException("Email không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentException("Tài khoản chưa có email.");
            }

            var lastOtp = await _userRepository.GetLatestPasswordResetOtpAsync(user.Id);
            if (lastOtp != null)
            {
                var seconds = (DateTime.Now - lastOtp.CreatedAt).TotalSeconds;
                if (seconds < 60)
                {
                    var remaining = 60 - (int)seconds;
                    throw new ArgumentException($"Vui lòng chờ {remaining} giây trước khi gửi OTP mới.");
                }
            }

            await _userRepository.InvalidateOldPasswordResetOtpsAsync(user.Id);

            var otp = Random.Shared.Next(100000, 1000000).ToString();
            var resetOtp = new PasswordResetOtp
            {
                UserId = user.Id,
                Otp = otp,
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsVerified = false,
                FailedAttempts = 0
            };

            await _userRepository.CreatePasswordResetOtpAsync(resetOtp);

            var job = new EmailJob(
                _emailService,
                user.Email,
                "Mã OTP đặt lại mật khẩu - English Center",
                $"Xin chào {user.UserName},\n\n" +
                $"Mã OTP đặt lại mật khẩu của bạn là: {otp}\n\n" +
                "Mã OTP có hiệu lực trong 5 phút.\n" +
                "Bạn chỉ được nhập sai tối đa 5 lần.\n\n" +
                "Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email."
            );

            _queue.Enqueue(job);
            return otp;
        }

        public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Otp))
            {
                throw new ArgumentException("Mã OTP không được để trống.");
            }

            if (dto.Otp.Length != 6 || !dto.Otp.All(char.IsDigit))
            {
                throw new ArgumentException("Mã OTP phải gồm 6 chữ số.");
            }

            var user = await _userRepository.GetByUserNameOrEmailAsync(dto.Email);
            if (user == null)
            {
                throw new ArgumentException("Email không tồn tại.");
            }

            var resetOtp = await _userRepository.GetLatestPasswordResetOtpAsync(user.Id);
            if (resetOtp == null || resetOtp.IsVerified)
            {
                throw new ArgumentException("Không tìm thấy mã OTP.");
            }

            if (resetOtp.ExpiredAt <= DateTime.Now)
            {
                throw new ArgumentException("Mã OTP đã hết hạn.");
            }

            if (resetOtp.FailedAttempts >= 5)
            {
                throw new ArgumentException("Bạn đã nhập sai OTP quá 5 lần.");
            }

            if (resetOtp.Otp != dto.Otp)
            {
                resetOtp.FailedAttempts++;
                await _userRepository.UpdatePasswordResetOtpAsync(resetOtp);
                throw new ArgumentException($"Mã OTP không chính xác. Bạn còn {5 - resetOtp.FailedAttempts} lần thử.");
            }

            resetOtp.IsVerified = true;
            await _userRepository.UpdatePasswordResetOtpAsync(resetOtp);

            return true;
        }

        public async Task<bool> SendLoginOtpAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Tên đăng nhập không được để trống.");
            }

            var user = await _userRepository.GetByUserNameOrEmailAsync(userName);
            if (user == null)
            {
                throw new ArgumentException("Tài khoản không tồn tại.");
            }

            await CreateAndQueueLoginOtpAsync(user);
            return true;
        }

        private async Task CreateAndQueueLoginOtpAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentException("Tài khoản chưa có email.");
            }

            var lastOtp = await _userRepository.GetLatestLoginOtpAsync(user.Id);
            if (lastOtp != null)
            {
                var seconds = (DateTime.Now - lastOtp.CreatedAt).TotalSeconds;
                if (seconds < 60)
                {
                    var remaining = 60 - (int)seconds;
                    throw new ArgumentException($"Vui lòng chờ {remaining} giây trước khi gửi OTP mới.");
                }
            }

            await _userRepository.InvalidateOldLoginOtpsAsync(user.Id);

            var otp = Random.Shared.Next(100000, 1000000).ToString();
            var loginOtp = new LoginOtp
            {
                UserId = user.Id,
                Otp = otp,
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsVerified = false,
                FailedAttempts = 0
            };

            await _userRepository.CreateLoginOtpAsync(loginOtp);

            var job = new EmailJob(
                _emailService,
                user.Email,
                "Mã xác minh đăng nhập - English Center",
                $"Xin chào {user.UserName},\n\n" +
                $"Mã OTP đăng nhập của bạn là: {otp}\n\n" +
                "Mã có hiệu lực trong 5 phút.\n" +
                "Bạn được nhập sai tối đa 5 lần.\n\n" +
                "Nếu bạn không thực hiện đăng nhập, hãy bỏ qua email."
            );

            _queue.Enqueue(job);
        }

        public async Task<LoginResultDto?> VerifyLoginOtpAsync(VerifyLoginOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException("Tên đăng nhập không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Otp))
            {
                throw new ArgumentException("Mã OTP không được để trống.");
            }

            if (dto.Otp.Length != 6 || !dto.Otp.All(char.IsDigit))
            {
                throw new ArgumentException("Mã OTP phải gồm 6 chữ số.");
            }

            var user = await _userRepository.GetByUserNameOrEmailAsync(dto.UserName);
            if (user == null)
            {
                throw new ArgumentException("Tài khoản không tồn tại.");
            }

            var loginOtp = await _userRepository.GetLatestLoginOtpAsync(user.Id);
            if (loginOtp == null || loginOtp.IsVerified)
            {
                throw new ArgumentException("Không tìm thấy mã OTP đăng nhập.");
            }

            if (loginOtp.ExpiredAt <= DateTime.Now)
            {
                loginOtp.IsVerified = true;
                await _userRepository.UpdateLoginOtpAsync(loginOtp);
                throw new ArgumentException("Mã OTP đã hết hạn.");
            }

            if (loginOtp.FailedAttempts >= 5)
            {
                loginOtp.IsVerified = true;
                await _userRepository.UpdateLoginOtpAsync(loginOtp);
                throw new ArgumentException("Bạn đã nhập sai OTP quá 5 lần.");
            }

            if (loginOtp.Otp != dto.Otp)
            {
                loginOtp.FailedAttempts++;
                if (loginOtp.FailedAttempts >= 5)
                {
                    loginOtp.IsVerified = true;
                    await _userRepository.UpdateLoginOtpAsync(loginOtp);
                    throw new ArgumentException("Bạn đã nhập sai OTP quá 5 lần. Mã OTP đã bị vô hiệu hóa.");
                }

                await _userRepository.UpdateLoginOtpAsync(loginOtp);
                throw new ArgumentException($"Mã OTP không chính xác. Bạn còn {5 - loginOtp.FailedAttempts} lần thử.");
            }

            loginOtp.IsVerified = true;
            await _userRepository.UpdateLoginOtpAsync(loginOtp);

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
