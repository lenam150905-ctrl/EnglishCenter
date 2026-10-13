const CONFIG = {
    // Khi chạy local dùng Gateway local HTTPS; khi deploy dùng cùng origin HTTPS.
    API_BASE_URL: ["localhost", "127.0.0.1"].includes(window.location.hostname)
        ? "https://localhost:7300/gateway/v1"
        : `${window.location.origin}/gateway/v1`,
    REQUEST_TIMEOUT: 15000
};
