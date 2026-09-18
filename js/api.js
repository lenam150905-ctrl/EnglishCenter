// =========================
// API HELPER
// =========================

async function apiRequest(endpoint, options = {}) {
    const controller = new AbortController();

    const timeoutId = window.setTimeout(
        function () {
            controller.abort();
        },
        CONFIG.REQUEST_TIMEOUT
    );

    try {
        const token = getAccessToken();

        const headers = {
            ...(options.headers || {})
        };

        // Chỉ thêm Content-Type khi có body
        if (
            options.body &&
            !(options.body instanceof FormData)
        ) {
            headers["Content-Type"] =
                headers["Content-Type"] ||
                "application/json";
        }

        // Nếu đã đăng nhập thì gửi JWT
        if (token) {
            headers["Authorization"] =
                `Bearer ${token}`;
        }

        const response = await fetch(
            `${CONFIG.API_BASE_URL}${endpoint}`,
            {
                ...options,
                headers: headers,
                signal: controller.signal
            }
        );

        let data = null;

        const contentType =
            response.headers.get("content-type");

        if (
            contentType &&
            contentType.includes("application/json")
        ) {
            data = await response.json();
        }

        // Token hết hạn / không hợp lệ
        if (response.status === 401) {
            // Endpoint đăng nhập cũng trả 401 khi sai thông tin; không báo
            // nhầm là phiên hết hạn và không xóa dữ liệu đăng nhập ở đây.
            if (endpoint.toLowerCase().startsWith("/auth/")) {
                throw new Error(
                    data?.message ||
                    "Tên đăng nhập hoặc mật khẩu không đúng."
                );
            }

            throw new Error(
                "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại nếu lỗi tiếp diễn."
            );
        }

        // Không có quyền
        if (response.status === 403) {
            throw new Error(
                "Bạn không có quyền thực hiện chức năng này."
            );
        }

        if (!response.ok) {
            throw new Error(
                data?.message ||
                data?.title ||
                `Lỗi ${response.status}: Không thể xử lý yêu cầu.`
            );
        }

        return data;

    } catch (error) {
        if (error.name === "AbortError") {
            throw new Error(
                "Máy chủ phản hồi quá lâu. Vui lòng thử lại."
            );
        }

        throw error;

    } finally {
        window.clearTimeout(timeoutId);
    }
}

// =========================
// GET
// =========================

function apiGet(endpoint) {
    return apiRequest(endpoint, {
        method: "GET"
    });
}

// =========================
// POST
// =========================

function apiPost(endpoint, body) {
    return apiRequest(endpoint, {
        method: "POST",
        body: JSON.stringify(body)
    });
}

// =========================
// PUT
// =========================

async function apiPut(endpoint, body) {
    const result = await apiRequest(endpoint, {
        method: "PUT",
        body: JSON.stringify(body)
    });
    if (window.showToast) window.showToast(endpoint.includes("/restore") ? "Khôi phục thành công." : "Cập nhật thành công.", "success");
    return result;
}

// =========================
// PATCH
// =========================

function apiPatch(endpoint, body) {
    return apiRequest(endpoint, {
        method: "PATCH",
        body: JSON.stringify(body)
    });
}

// =========================
// DELETE
// =========================

async function apiDelete(endpoint) {
    const result = await apiRequest(endpoint, {
        method: "DELETE"
    });
    if (window.showToast) window.showToast("Xóa thành công.", "success");
    return result;
}

async function apiDownload(endpoint, fileName) {
    const response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, {
        method: "GET",
        headers: getAccessToken() ? { "Authorization": `Bearer ${getAccessToken()}` } : {}
    });
    if (!response.ok) {
        let data = null;
        try { data = await response.json(); } catch (_) { }
        throw new Error(data?.message || "Không thể tải tệp PDF.");
    }
    const url = URL.createObjectURL(await response.blob());
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName || "chung-chi.pdf";
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
}
