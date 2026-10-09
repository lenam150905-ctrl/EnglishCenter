

function getAuthStorage() {
    if (localStorage.getItem("accessToken")) {
        return localStorage;
    }

    if (sessionStorage.getItem("accessToken")) {
        return sessionStorage;
    }

    return null;
}

function getAccessToken() {
    return (
        localStorage.getItem("accessToken") ||
        sessionStorage.getItem("accessToken")
    );
}

function getCurrentUserName() {
    return (
        localStorage.getItem("userName") ||
        sessionStorage.getItem("userName") ||
        ""
    );
}

function getCurrentRole() {
    return (
        localStorage.getItem("role") ||
        sessionStorage.getItem("role") ||
        ""
    );
}

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

function isAuthenticated() {
    return !!getAccessToken();
}

function saveAuthData(data, token, rememberMe = false) {
    
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

async function logout() {
    if (window.appConfirm && !(await window.appConfirm("Bạn có chắc muốn đăng xuất?", "Đăng xuất"))) {
        return;
    }
    clearAuthData();

    window.location.href = "/index.html";
}

function requireAuth() {
    if (!isAuthenticated()) {
        window.location.href = "/index.html";
        return false;
    }

    return true;
}

function hasRole(...roles) {
    const currentRole = getCurrentRole();

    return roles.includes(currentRole);
}

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

function preserveAuthForPaymentReturn() {
    ["accessToken", "userName", "role", "currentUser"].forEach(function (key) {
        const value = localStorage.getItem(key) || sessionStorage.getItem(key);
        if (value) localStorage.setItem(key, value);
    });
}

