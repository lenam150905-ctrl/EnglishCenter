// =========================
// Cấu hình chung hệ thống
// =========================

const CONFIG = {
    // Frontend chỉ gọi API Gateway. Gateway chuyển tiếp request này
    // đến EnglishCenter API nội bộ tại https://localhost:7207/api.
    API_BASE_URL: "https://localhost:7300/gateway",
    REQUEST_TIMEOUT: 15000
};
