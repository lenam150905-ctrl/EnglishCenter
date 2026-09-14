// =========================
// Lấy phần tử HTML
// =========================

const loginForm =
    document.getElementById("loginForm");

const loginInput =
    document.getElementById("loginInput");

const passwordInput =
    document.getElementById("password");

const rememberMeInput =
    document.getElementById("rememberMe");

const loginButton =
    document.getElementById("loginButton");

const loginButtonText =
    document.getElementById("loginButtonText");

const loginLoading =
    document.getElementById("loginLoading");

const loginMessage =
    document.getElementById("loginMessage");

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

    loginMessage.className =
        `login-message ${type}`;
}

function showError(message) {
    showMessage(message, "error");
}

function hideMessage() {
    loginMessage.textContent = "";
    loginMessage.className = "login-message";
}


// =========================
// Loading
// =========================

function setLoading(isLoading) {
    loginButton.disabled = isLoading;

    if (isLoading) {
        loginButtonText.textContent =
            "Đang đăng nhập...";

        loginLoading.classList.remove("hidden");
    } else {
        loginButtonText.textContent =
            "Đăng nhập";

        loginLoading.classList.add("hidden");
    }
}


// =========================
// Xử lý đăng nhập
// =========================

loginForm.addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();

        const loginValue =
            loginInput.value.trim();

        const password =
            passwordInput.value;

        if (
            loginValue === "" ||
            password === ""
        ) {
            showError(
                "Vui lòng nhập đầy đủ thông tin đăng nhập."
            );

            return;
        }

        setLoading(true);
        hideMessage();

        try {

            const data = await apiPost(
                "/Auth/login",
                {
                    userName: loginValue,
                    password: password
                }
            );

            // =========================
            // Kiểm tra OTP
            // =========================

            const requiresTwoFactor =
                data?.requiresTwoFactor === true ||
                data?.RequiresTwoFactor === true;

            if (requiresTwoFactor) {

                sessionStorage.setItem(
                    "pendingLoginUserName",
                    data.userName ||
                    data.UserName ||
                    loginValue
                );

                /*
                 * Quan trọng:
                 * Lưu trạng thái rememberMe
                 * để sau khi xác thực OTP biết
                 * nên dùng localStorage hay sessionStorage.
                 */
                sessionStorage.setItem(
                    "pendingRememberMe",
                    rememberMeInput.checked
                        ? "true"
                        : "false"
                );

                window.location.href =
                    "pages/verify-login-otp.html";

                return;
            }

            // =========================
            // Lấy token
            // =========================

            const auth =
                data.auth ||
                data.Auth ||
                {};

            const token =
                auth.token ||
                auth.Token ||
                data.token ||
                data.Token ||
                data.accessToken ||
                data.AccessToken;

            if (!token) {
                showError(
                    "Đăng nhập thành công nhưng không nhận được token."
                );

                return;
            }

            // =========================
            // Lưu Auth
            // =========================

            saveAuthData(
                data,
                token,
                rememberMeInput.checked
            );

            // =========================
            // Chuyển Dashboard
            // =========================

            redirectToDashboard();

        } catch (error) {

            console.error(
                "Login error:",
                error
            );

            showError(
                error.message ||
                "Không thể kết nối đến máy chủ."
            );

        } finally {

            setLoading(false);
        }
    }
);


// =========================
// Hiện / ẩn mật khẩu
// =========================

togglePasswordButton.addEventListener(
    "click",
    function () {

        const isPassword =
            passwordInput.type === "password";

        passwordInput.type =
            isPassword
                ? "text"
                : "password";

        togglePasswordButton.textContent =
            isPassword
                ? "🙈"
                : "👁";
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
// Đăng ký
// =========================

registerLink.addEventListener(
    "click",
    function (event) {

        event.preventDefault();

        window.location.href =
            "pages/register.html";
    }
);