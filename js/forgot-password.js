// =========================
// LẤY PHẦN TỬ HTML
// =========================

const forms = [
    document.getElementById("forgotForm"),
    document.getElementById("otpForm"),
    document.getElementById("resetForm")
];

const steps =
    document.querySelectorAll(
        ".progress-steps span"
    );

const title =
    document.getElementById(
        "recoveryTitle"
    );

const description =
    document.getElementById(
        "recoveryDescription"
    );

const message =
    document.getElementById(
        "recoveryMessage"
    );

const emailInput =
    document.getElementById(
        "recoveryEmail"
    );

const otpInput =
    document.getElementById(
        "recoveryOtp"
    );

const newPasswordInput =
    document.getElementById(
        "newPassword"
    );

const confirmPasswordInput =
    document.getElementById(
        "confirmPassword"
    );


// =========================
// BIẾN TẠM
// =========================

let recoveryEmail = "";
let recoveryOtp = "";


// =========================
// NỘI DUNG TỪNG BƯỚC
// =========================

const copy = [
    [
        "Quên mật khẩu?",
        "Nhập email để nhận mã OTP gồm 6 chữ số."
    ],
    [
        "Kiểm tra email",
        "Nhập mã OTP vừa được gửi đến email của bạn."
    ],
    [
        "Tạo mật khẩu mới",
        "Chọn mật khẩu mới có ít nhất 6 ký tự."
    ]
];


// =========================
// HIỂN THỊ THÔNG BÁO
// =========================

function showMessage(
    text,
    type = "error"
) {

    message.textContent =
        text;

    message.className =
        "login-message " + type;
}


// =========================
// CHUYỂN BƯỚC
// =========================

function setStep(step) {

    forms.forEach(
        (form, index) => {

            form.classList.toggle(
                "active",
                index === step
            );
        }
    );


    steps.forEach(
        (item, index) => {

            item.classList.toggle(
                "active",
                index <= step
            );
        }
    );


    title.textContent =
        copy[step][0];

    description.textContent =
        copy[step][1];


    showMessage("", "");


    if (step === 1) {
        otpInput.focus();
    }


    if (step === 2) {
        newPasswordInput.focus();
    }
}


// =========================
// CHỈ CHO NHẬP OTP 6 SỐ
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
// BƯỚC 1
// GỬI OTP QUÊN MẬT KHẨU
// =========================

forms[0].addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();


        recoveryEmail =
            emailInput.value.trim();


        if (recoveryEmail === "") {

            showMessage(
                "Vui lòng nhập email."
            );

            return;
        }


        try {

            await apiPost(
                "/Auth/forgot-password",
                {
                    email:
                        recoveryEmail
                }
            );


            setStep(1);


            showMessage(
                "Mã OTP đã được gửi. Mã có hiệu lực trong 5 phút.",
                "success"
            );

        } catch (error) {

            console.error(
                "Forgot password error:",
                error
            );


            showMessage(
                error.message ||
                "Không thể gửi mã OTP."
            );
        }
    }
);


// =========================
// BƯỚC 2
// XÁC THỰC OTP
// =========================

forms[1].addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();


        recoveryOtp =
            otpInput.value.trim();


        if (
            !/^\d{6}$/.test(
                recoveryOtp
            )
        ) {

            showMessage(
                "Mã OTP phải gồm đúng 6 chữ số."
            );

            return;
        }


        try {

            await apiPost(
                "/Auth/verify-otp",
                {
                    email:
                        recoveryEmail,

                    otp:
                        recoveryOtp
                }
            );


            setStep(2);

        } catch (error) {

            console.error(
                "Verify recovery OTP error:",
                error
            );


            showMessage(
                error.message ||
                "Mã OTP không đúng hoặc đã hết hạn."
            );
        }
    }
);


// =========================
// BƯỚC 3
// ĐẶT LẠI MẬT KHẨU
// =========================

forms[2].addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();


        const newPassword =
            newPasswordInput.value;

        const confirmPassword =
            confirmPasswordInput.value;


        if (
            newPassword.length < 6
        ) {

            showMessage(
                "Mật khẩu mới phải có ít nhất 6 ký tự."
            );

            return;
        }


        if (
            newPassword !==
            confirmPassword
        ) {

            showMessage(
                "Mật khẩu xác nhận không khớp."
            );

            return;
        }


        try {

            await apiPost(
                "/Auth/reset-password",
                {
                    email:
                        recoveryEmail,

                    otp:
                        recoveryOtp,

                    newPassword:
                        newPassword,

                    confirmPassword:
                        confirmPassword
                }
            );


            showMessage(
                "Đặt lại mật khẩu thành công. Đang chuyển đến trang đăng nhập…",
                "success"
            );


            window.setTimeout(
                function () {

                    window.location.href =
                        "../index.html";

                },
                1200
            );

        } catch (error) {

            console.error(
                "Reset password error:",
                error
            );


            showMessage(
                error.message ||
                "Không thể đặt lại mật khẩu."
            );
        }
    }
);