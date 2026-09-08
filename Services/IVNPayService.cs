namespace EnglishCenter.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(
            int invoiceId,
            decimal amount,
            string orderInfo,
            string ipAddress);

        bool ValidateResponse(
            IQueryCollection query);
    }
}