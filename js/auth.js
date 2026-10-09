
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
    // Credentials are held in an HttpOnly cookie and cannot be read by JS.
    // The non-sensitive profile metadata below only drives the client UI.
    return !!getCurrentUserName() && !!getCurrentRole();
}

function saveAuthData(data, rememberMe = false) {
    
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
        // Removes tokens saved by older releases during the first new login.
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
    try {
        if (typeof apiPost === "function") {
            await apiPost("/Auth/logout", {});
        }
    } catch (error) {
        // Clearing the local UI state is still safe if a previous session has
        // already expired on the server.
        console.warn("Logout request failed:", error);
    } finally {
        clearAuthData();
        window.location.href = "/index.html";
    }
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
    ["userName", "role", "currentUser"].forEach(function (key) {
        const value = localStorage.getItem(key) || sessionStorage.getItem(key);
        if (value) localStorage.setItem(key, value);
    });
}

