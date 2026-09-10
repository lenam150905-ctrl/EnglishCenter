namespace EnglishCenter.API.Models
{
    public class LoginOtp
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Otp { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiredAt { get; set; }

        public bool IsVerified { get; set; } = false;

        public int FailedAttempts { get; set; } = 0;

        public User User { get; set; } = null!;
    }
}