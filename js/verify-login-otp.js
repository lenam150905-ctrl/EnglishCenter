const API_BASE_URL = "https://localhost:7207/api";

const otpForm = document.getElementById("otpForm");
const otpInput = document.getElementById("otpInput");
const otpError = document.getElementById("otpError");

const verifyOtpButton = document.getElementById("verifyOtpButton");
const buttonText = document.getElementById("buttonText");
const buttonLoading = document.getElementById("buttonLoading");

const resendOtpButton = document.getElementById("resendOtpButton");

otpInput.addEventListener("input", function () {
    this.value = this.value.replace(/\D/g, "").slice(0, 6);
});

const pendingLoginUserName =
    sessionStorage.getItem("pendingLoginUserName");

if (!pendingLoginUserName) {
    window.location.href = "../index.html";
}

function showOtpError(message) {
    otpError.textContent = message;
    otpError.style.display = "block";
}

function hideOtpError() {
    otpError.textContent = "";
    otpError.style.display = "none";
}

function setLoading(isLoading) {
    verifyOtpButton.disabled = isLoading;

    if (isLoading) {
        buttonText.style.display = "none";
        buttonLoading.style.display = "inline";
    } else {
        buttonText.style.display = "inline";
        buttonLoading.style.display = "none";
    }
}

function saveLoginData(data, token) {
    const auth = data.auth || data.Auth || {};

    const userName =
        data.userName ||
        data.UserName ||
        auth.userName ||
        auth.UserName ||
        pendingLoginUserName;

    const role =
        data.role ||
        data.Role ||
        auth.role ||
        auth.Role ||
        "";

    localStorage.setItem("accessToken", token);
    localStorage.setItem("userName", userName);
    localStorage.setItem("role", role);

    localStorage.setItem("currentUser", JSON.stringify({
        userName: userName,
        role: role
    }));
}

otpForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const otp = otpInput.value.trim();

    if (otp === "") {
        showOtpError("Vui lòng nhập mã OTP.");
        return;
    }

    if (!/^\d{6}$/.test(otp)) {
        showOtpError("Mã OTP phải gồm 6 chữ số.");
        return;
    }

    hideOtpError();
    setLoading(true);

    try {
        const response = await fetch(
            `${API_BASE_URL}/Auth/verify-login-otp`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    userName: pendingLoginUserName,
                    otp: otp
                })
            }
        );

        const data = await response.json();

        if (!response.ok) {
            showOtpError(
                data.message || "Mã OTP không đúng hoặc đã hết hạn."
            );
            return;
        }

        const auth = data.auth || data.Auth || {};

        const token =
            auth.token ||
            auth.Token ||
            data.token ||
            data.Token ||
            data.accessToken ||
            data.AccessToken;

        if (!token) {
            showOtpError("Xác thực OTP thành công nhưng không nhận được token.");
            return;
        }

        saveLoginData(data, token);

        sessionStorage.removeItem("pendingLoginUserName");

        window.location.href = "dashboard.html";

    } catch (error) {
        console.error("Verify OTP error:", error);
        showOtpError("Không thể kết nối đến máy chủ.");
    } finally {
        setLoading(false);
    }
});

resendOtpButton.addEventListener("click", async function () {
    if (!pendingLoginUserName) {
        showOtpError("Không tìm thấy thông tin tài khoản.");
        return;
    }

    try {
        const response = await fetch(
            `${API_BASE_URL}/Auth/send-login-otp`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    userName: pendingLoginUserName
                })
            }
        );

        const data = await response.json();

        if (!response.ok) {
            showOtpError(data.message || "Không thể gửi lại mã OTP.");
            return;
        }

        alert("Mã OTP mới đã được gửi đến email của bạn.");

    } catch (error) {
        console.error("Resend OTP error:", error);
        showOtpError("Không thể kết nối đến máy chủ.");
    }
});
