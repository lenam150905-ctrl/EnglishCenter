// =========================
// AUTH HELPER
// =========================

function getAuthStorage() {
    if (localStorage.getItem("accessToken")) {
        return localStorage;
    }

    if (sessionStorage.getItem("accessToken")) {
        return sessionStorage;
    }

    return null;
}

// =========================
// Lấy token
// =========================

function getAccessToken() {
    return (
        localStorage.getItem("accessToken") ||
        sessionStorage.getItem("accessToken")
    );
}

// =========================
// Lấy username
// =========================

function getCurrentUserName() {
    return (
        localStorage.getItem("userName") ||
        sessionStorage.getItem("userName") ||
        ""
    );
}

// =========================
// Lấy role
// =========================

function getCurrentRole() {
    return (
        localStorage.getItem("role") ||
        sessionStorage.getItem("role") ||
        ""
    );
}

// =========================
// Lấy currentUser
// =========================

function getCurrentUser() {
    const rawUser =
        localStorage.getItem("currentUser") ||
        sessionStorage.getItem("currentUser");

    if (!rawUser) {
        return null;
    }

    try {
        return JSON.parse(rawUser);
    } catch {
        return null;
    }
}

// =========================
// Kiểm tra đã đăng nhập
// =========================

function isAuthenticated() {
    return !!getAccessToken();
}

// =========================
// Lưu dữ liệu đăng nhập
// =========================

function saveAuthData(data, token, rememberMe = false) {
    // Xóa dữ liệu cũ để tránh tồn tại ở cả 2 storage
    clearAuthData();

    const storage = rememberMe
        ? localStorage
        : sessionStorage;

    const auth = data.auth || data.Auth || {};

    const userName =
        data.userName ||
        data.UserName ||
        auth.userName ||
        auth.UserName ||
        "";

    const role =
        data.role ||
        data.Role ||
        auth.role ||
        auth.Role ||
        "";

    storage.setItem("accessToken", token);
    storage.setItem("userName", userName);
    storage.setItem("role", role);

    storage.setItem(
        "currentUser",
        JSON.stringify({
            userName: userName,
            role: role
        })
    );
}

// =========================
// Xóa dữ liệu đăng nhập
// =========================

function clearAuthData() {
    const keys = [
        "accessToken",
        "userName",
        "role",
        "currentUser"
    ];

    keys.forEach(function (key) {
        localStorage.removeItem(key);
        sessionStorage.removeItem(key);
    });
}

// =========================
// Logout
// =========================

function logout() {
    clearAuthData();

    window.location.href = "/index.html";
}

// =========================
// Yêu cầu đăng nhập
// =========================

function requireAuth() {
    if (!isAuthenticated()) {
        window.location.href = "/index.html";
        return false;
    }

    return true;
}

// =========================
// Kiểm tra role
// =========================

function hasRole(...roles) {
    const currentRole = getCurrentRole();

    return roles.includes(currentRole);
}

// =========================
// Bảo vệ trang theo role
// =========================

function requireRole(...roles) {
    if (!requireAuth()) {
        return false;
    }

    const currentRole = getCurrentRole();

    if (!roles.includes(currentRole)) {
        alert("Bạn không có quyền truy cập trang này.");

        redirectToDashboard();
        return false;
    }

    return true;
}

// =========================
// Điều hướng Dashboard
// =========================

function redirectToDashboard() {
    const role = getCurrentRole();

    switch (role) {
        case "Admin":
            window.location.href =
                "/pages/admin/dashboard.html";
            break;

        case "Teacher":
            window.location.href =
                "/pages/teacher/dashboard.html";
            break;

        case "Student":
            window.location.href =
                "/pages/student/dashboard.html";
            break;

        default:
            clearAuthData();
            window.location.href =
                "/index.html";
            break;
    }
}
