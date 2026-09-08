using EnglishCenter.API.Models;
using EnglishCenter.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EnglishCenter.API.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly VNPaySettings _settings;

        public VNPayService(
            IOptions<VNPaySettings> options)
        {
            _settings = options.Value;
        }

        public string CreatePaymentUrl(
            int invoiceId,
            decimal amount,
            string orderInfo,
            string ipAddress)
        {
            if (invoiceId <= 0)
            {
                throw new ArgumentException(
                    "InvoiceId không hợp lệ.");
            }

            if (amount <= 0)
            {
                throw new ArgumentException(
                    "Số tiền thanh toán phải lớn hơn 0.");
            }

            if (string.IsNullOrWhiteSpace(_settings.TmnCode))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình VNPay:TmnCode.");
            }

            if (string.IsNullOrWhiteSpace(_settings.HashSecret))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình VNPay:HashSecret.");
            }

            if (string.IsNullOrWhiteSpace(_settings.PaymentUrl))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình VNPay:PaymentUrl.");
            }

            if (string.IsNullOrWhiteSpace(_settings.ReturnUrl))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình VNPay:ReturnUrl.");
            }

            var createDate = DateTime.Now;

            // Mã giao dịch duy nhất
            var txnRef =
                $"{invoiceId}_{createDate:yyyyMMddHHmmss}";

            var parameters =
                new SortedDictionary<string, string>(
                    StringComparer.Ordinal)
                {
                    ["vnp_Version"] = "2.1.0",
                    ["vnp_Command"] = "pay",
                    ["vnp_TmnCode"] = _settings.TmnCode,
                    ["vnp_Amount"] =
                        ((long)(amount * 100))
                        .ToString(
                            CultureInfo.InvariantCulture),

                    ["vnp_CreateDate"] =
                        createDate.ToString(
                            "yyyyMMddHHmmss"),

                    ["vnp_CurrCode"] = "VND",

                    ["vnp_IpAddr"] =
                        GetIpAddress(ipAddress),

                    ["vnp_Locale"] = "vn",

                    ["vnp_OrderInfo"] =
                        orderInfo,

                    ["vnp_OrderType"] = "other",

                    ["vnp_ReturnUrl"] =
                        _settings.ReturnUrl,

                    ["vnp_TxnRef"] = txnRef
                };

            var query = new StringBuilder();

            foreach (var item in parameters)
            {
                if (query.Length > 0)
                {
                    query.Append("&");
                }

                query.Append(
                    WebUtility.UrlEncode(item.Key));

                query.Append("=");

                query.Append(
                    WebUtility.UrlEncode(item.Value));
            }

            var hashData = query.ToString();

            var secureHash =
                HmacSha512(
                    _settings.HashSecret,
                    hashData);

            return $"{_settings.PaymentUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateResponse(
            IQueryCollection query)
        {
            if (query == null)
            {
                return false;
            }

            var secureHash =
                query["vnp_SecureHash"]
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(secureHash))
            {
                return false;
            }

            var parameters =
                new SortedDictionary<string, string>(
                    StringComparer.Ordinal);

            foreach (var item in query)
            {
                if (item.Key.StartsWith("vnp_") &&
                    item.Key != "vnp_SecureHash" &&
                    item.Key != "vnp_SecureHashType")
                {
                    var value = item.Value
                        .FirstOrDefault();

                    if (value != null)
                    {
                        parameters[item.Key] = value;
                    }
                }
            }

            var queryBuilder =
                new StringBuilder();

            foreach (var item in parameters)
            {
                if (queryBuilder.Length > 0)
                {
                    queryBuilder.Append("&");
                }

                queryBuilder.Append(
                    WebUtility.UrlEncode(item.Key));

                queryBuilder.Append("=");

                queryBuilder.Append(
                    WebUtility.UrlEncode(item.Value));
            }

            var calculatedHash =
                HmacSha512(
                    _settings.HashSecret,
                    queryBuilder.ToString());

            return string.Equals(
                calculatedHash,
                secureHash,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha512(
            string key,
            string data)
        {
            var keyBytes =
                Encoding.UTF8.GetBytes(key);

            var dataBytes =
                Encoding.UTF8.GetBytes(data);

            using var hmac =
                new HMACSHA512(keyBytes);

            var hash =
                hmac.ComputeHash(dataBytes);

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        private static string GetIpAddress(
            string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return "127.0.0.1";
            }

            if (ipAddress == "::1")
            {
                return "127.0.0.1";
            }

            return ipAddress;
        }
    }
}