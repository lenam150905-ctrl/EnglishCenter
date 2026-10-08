

async function apiRequest(endpoint, options = {}) {
    const controller = new AbortController();

    const timeoutId = window.setTimeout(
        function () {
            controller.abort();
        },
        CONFIG.REQUEST_TIMEOUT
    );

    try {
        // Public pages (for example Forgot Password) do not load auth.js.
        // They must still be able to call anonymous API endpoints.
        const token = typeof getAccessToken === "function"
            ? getAccessToken()
            : null;

        const headers = {
            ...(options.headers || {})
        };

if (
            options.body &&
            !(options.body instanceof FormData)
        ) {
            headers["Content-Type"] =
                headers["Content-Type"] ||
                "application/json";
        }

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

if (response.status === 401) {

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

if (response.status === 403) {
            throw new Error(
                "Bạn không có quyền thực hiện chức năng này."
            );
        }

        if (!response.ok) {
            const validationErrors = data?.errors && typeof data.errors === "object"
                ? Object.values(data.errors).flat().filter(Boolean)
                : [];
            throw new Error(
                validationErrors.join(" ") ||
                data?.message ||
                data?.detail ||
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

function apiGet(endpoint) {
    return apiRequest(endpoint, {
        method: "GET"
    });
}

function apiPost(endpoint, body) {
    return apiRequest(endpoint, {
        method: "POST",
        body: JSON.stringify(body)
    });
}

async function apiPut(endpoint, body) {
    const result = await apiRequest(endpoint, {
        method: "PUT",
        body: JSON.stringify(body)
    });
    if (window.showToast) window.showToast(endpoint.includes("/restore") ? "Khôi phục thành công." : "Cập nhật thành công.", "success");
    return result;
}

function apiPatch(endpoint, body) {
    return apiRequest(endpoint, {
        method: "PATCH",
        body: JSON.stringify(body)
    });
}

async function apiDelete(endpoint) {
    const result = await apiRequest(endpoint, {
        method: "DELETE"
    });
    if (window.showToast) window.showToast("Xóa thành công.", "success");
    return result;
}

async function apiDownload(endpoint, fileName) {
    const token = typeof getAccessToken === "function"
        ? getAccessToken()
        : null;

    const response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, {
        method: "GET",
        headers: token ? { "Authorization": `Bearer ${token}` } : {}
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


