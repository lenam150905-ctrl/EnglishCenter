

const otpForm =
    document.getElementById("otpForm");

const otpInput =
    document.getElementById("otpInput");

const otpError =
    document.getElementById("otpError");

const verifyOtpButton =
    document.getElementById("verifyOtpButton");

const buttonText =
    document.getElementById("buttonText");

const buttonLoading =
    document.getElementById("buttonLoading");

const resendOtpButton =
    document.getElementById("resendOtpButton");

const pendingLoginUserName =
    sessionStorage.getItem(
        "pendingLoginUserName"
    );

const pendingRememberMe =
    sessionStorage.getItem(
        "pendingRememberMe"
    ) === "true";

if (!pendingLoginUserName) {

    window.location.href =
        "../index.html";
}

otpInput.addEventListener(
    "input",
    function () {

        this.value =
            this.value
                .replace(/\D/g, "")
                .slice(0, 6);
    }
);

function showOtpError(message) {

    otpError.textContent =
        message;

    otpError.style.display =
        "block";
}

function hideOtpError() {

    otpError.textContent =
        "";

    otpError.style.display =
        "none";
}

function setOtpLoading(isLoading) {

    verifyOtpButton.disabled =
        isLoading;

if (isLoading) {

        buttonText.style.display =
            "none";

        buttonLoading.style.display =
            "inline";

    } else {

        buttonText.style.display =
            "inline";

        buttonLoading.style.display =
            "none";
    }
}

otpForm.addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();

const otp =
            otpInput.value.trim();

if (otp === "") {

            showOtpError(
                "Vui lòng nhập mã OTP."
            );

            return;
        }

if (!/^\d{6}$/.test(otp)) {

            showOtpError(
                "Mã OTP phải gồm 6 chữ số."
            );

            return;
        }

hideOtpError();

        setOtpLoading(true);

try {

const data =
                await apiPost(
                    "/Auth/verify-login-otp",
                    {
                        userName:
                            pendingLoginUserName,

                        otp: otp
                    }
                );

saveAuthData(
                data,
                pendingRememberMe
            );

sessionStorage.removeItem(
                "pendingLoginUserName"
            );

            sessionStorage.removeItem(
                "pendingRememberMe"
            );

redirectToDashboard();

        } catch (error) {

            console.error(
                "Verify OTP error:",
                error
            );

showOtpError(
                error.message ||
                "Không thể xác thực mã OTP."
            );

        } finally {

            setOtpLoading(false);
        }
    }
);

resendOtpButton.addEventListener(
    "click",
    async function () {

        if (!pendingLoginUserName) {

            showOtpError(
                "Không tìm thấy thông tin tài khoản."
            );

            return;
        }

resendOtpButton.disabled =
            true;

try {

            await apiPost(
                "/Auth/send-login-otp",
                {
                    userName:
                        pendingLoginUserName
                }
            );

hideOtpError();

alert(
                "Mã OTP mới đã được gửi đến email của bạn."
            );

        } catch (error) {

            console.error(
                "Resend OTP error:",
                error
            );

showOtpError(
                error.message ||
                "Không thể gửi lại mã OTP."
            );

        } finally {

            resendOtpButton.disabled =
                false;
        }
    }
);

