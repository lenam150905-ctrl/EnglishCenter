// =========================
// LẤY PHẦN TỬ HTML
// =========================

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


// =========================
// THÔNG TIN LOGIN ĐANG CHỜ OTP
// =========================

const pendingLoginUserName =
    sessionStorage.getItem(
        "pendingLoginUserName"
    );

const pendingRememberMe =
    sessionStorage.getItem(
        "pendingRememberMe"
    ) === "true";


// Nếu không có username đang chờ OTP
// thì quay về Login

if (!pendingLoginUserName) {

    window.location.href =
        "../index.html";
}


// =========================
// CHỈ CHO NHẬP 6 CHỮ SỐ
// =========================

otpInput.addEventListener(
    "input",
    function () {

        this.value =
            this.value
                .replace(/\D/g, "")
                .slice(0, 6);
    }
);


// =========================
// HIỂN THỊ LỖI
// =========================

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


// =========================
// LOADING
// =========================

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


// =========================
// XÁC THỰC OTP
// =========================

otpForm.addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();


        const otp =
            otpInput.value.trim();


        // =========================
        // VALIDATE
        // =========================

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

            // =========================
            // GỌI API
            // =========================

            const data =
                await apiPost(
                    "/Auth/verify-login-otp",
                    {
                        userName:
                            pendingLoginUserName,

                        otp: otp
                    }
                );


            // =========================
            // LẤY TOKEN
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

                showOtpError(
                    "Xác thực OTP thành công nhưng không nhận được token."
                );

                return;
            }


            // =========================
            // LƯU AUTH
            // =========================

            saveAuthData(
                data,
                token,
                pendingRememberMe
            );


            // =========================
            // XÓA DỮ LIỆU TẠM
            // =========================

            sessionStorage.removeItem(
                "pendingLoginUserName"
            );

            sessionStorage.removeItem(
                "pendingRememberMe"
            );


            // =========================
            // ĐI DASHBOARD THEO ROLE
            // =========================

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


// =========================
// GỬI LẠI OTP
// =========================

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