const API_BASE_URL = "https://localhost:7207/api";

// =========================
// Lấy phần tử HTML
// =========================

const loginForm = document.getElementById("loginForm");

const loginInput = document.getElementById("loginInput");
const passwordInput = document.getElementById("password");
const rememberMeInput = document.getElementById("rememberMe");

const loginButton = document.getElementById("loginButton");
const loginButtonText = document.getElementById("loginButtonText");
const loginLoading = document.getElementById("loginLoading");
const loginMessage = document.getElementById("loginMessage");

const togglePasswordButton =
    document.getElementById("togglePassword");

const forgotPasswordLink =
    document.getElementById("forgotPassword");

const registerLink =
    document.getElementById("registerLink");


// =========================
// Hiển thị thông báo
// =========================

function showMessage(message, type = "error") {
    loginMessage.textContent = message;
    loginMessage.className = `login-message ${type}`;
}

function showError(message) {
    showMessage(message, "error");
}

function hideError() {
    loginMessage.textContent = "";
    loginMessage.className = "login-message";
}


// =========================
// Trạng thái loading
// =========================

function setLoading(isLoading) {
    loginButton.disabled = isLoading;

    if (isLoading) {
        loginButtonText.textContent = "Đang đăng nhập...";
        loginLoading.classList.remove("hidden");
    } else {
        loginButtonText.textContent = "Đăng nhập";
        loginLoading.classList.add("hidden");
    }
}


// =========================
// Lưu dữ liệu đăng nhập
// =========================

function saveLoginData(data, token, rememberMe) {
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

    storage.setItem("currentUser", JSON.stringify({
        userName: userName,
        role: role
    }));
}


// =========================
// Xử lý đăng nhập
// =========================

loginForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const loginValue = loginInput.value.trim();
    const password = passwordInput.value;

    if (loginValue === "" || password === "") {
        showError("Vui lòng nhập đầy đủ thông tin đăng nhập.");
        return;
    }

    setLoading(true);
    hideError();

    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => controller.abort(), 15000);

    try {
        // Tránh để giao diện ở trạng thái loading vô hạn khi API/SSL bị treo.
        const response = await fetch(`${API_BASE_URL}/Auth/login`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            signal: controller.signal,
            body: JSON.stringify({
                userName: loginValue,
                password: password
            })
        });

        const data = await response.json();

        if (!response.ok) {
            showError(data.message || "Tên đăng nhập hoặc mật khẩu không đúng.");
            return;
        }

        /*
         * Bước 1:
         * API yêu cầu xác thực OTP
         */
        const requiresTwoFactor =
            data.requiresTwoFactor === true ||
            data.RequiresTwoFactor === true;

        if (requiresTwoFactor) {
            sessionStorage.setItem(
                "pendingLoginUserName",
                data.userName || data.UserName || loginValue
            );

            window.location.href = "pages/verify-login-otp.html";
            return;
        }

        /*
         * Trường hợp API không yêu cầu OTP
         * thì mới lấy token trực tiếp
         */
        const auth = data.auth || data.Auth || {};

        const token =
            auth.token ||
            auth.Token ||
            data.token ||
            data.Token ||
            data.accessToken ||
            data.AccessToken;

        if (!token) {
            showError("Đăng nhập thành công nhưng không nhận được token.");
            return;
        }

        saveLoginData(data, token, rememberMeInput.checked);

        window.location.href = "pages/dashboard.html";

    } catch (error) {
        console.error("Login error:", error);
        showError(
            error.name === "AbortError"
                ? "Máy chủ phản hồi quá lâu. Vui lòng thử lại."
                : "Không thể kết nối đến máy chủ."
        );
    } finally {
        window.clearTimeout(timeoutId);
        setLoading(false);
    }
});


// =========================
// Hiện/ẩn mật khẩu
// =========================

togglePasswordButton.addEventListener(
    "click",
    function () {
        const isPassword =
            passwordInput.type === "password";

        passwordInput.type =
            isPassword ? "text" : "password";

        togglePasswordButton.textContent =
            isPassword ? "🙈" : "👁";
    }
);


// =========================
// Quên mật khẩu
// =========================

forgotPasswordLink.addEventListener(
    "click",
    function (event) {
        event.preventDefault();

        window.location.href =
            "pages/forgot-password.html";
    }
);


// =========================
// Đăng ký tài khoản
// =========================

registerLink.addEventListener(
    "click",
    function (event) {
        event.preventDefault();

        window.location.href =
            "pages/register.html";
    }
);
