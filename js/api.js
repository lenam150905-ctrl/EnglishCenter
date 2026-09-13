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
        if (options.body) {
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
            clearAuthData();

            throw new Error(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
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

function apiPut(endpoint, body) {
    return apiRequest(endpoint, {
        method: "PUT",
        body: JSON.stringify(body)
    });
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

function apiDelete(endpoint) {
    return apiRequest(endpoint, {
        method: "DELETE"
    });
}