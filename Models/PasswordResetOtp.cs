namespace EnglishCenter.API.Models
{
    public class PasswordResetOtp
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Otp { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiredAt { get; set; }

        public bool IsVerified { get; set; }

        public int FailedAttempts { get; set; }
        public User User { get; set; } = null!;
    }
}