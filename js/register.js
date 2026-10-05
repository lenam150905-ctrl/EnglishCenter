// =========================
// LẤY PHẦN TỬ HTML
// =========================

const registerForm =
    document.getElementById(
        "registerForm"
    );

const registerButton =
    document.getElementById(
        "registerButton"
    );

const registerToast = document.getElementById("registerToast");
const registerToastIcon = document.getElementById("registerToastIcon");
const registerToastTitle = document.getElementById("registerToastTitle");
const registerToastMessage = document.getElementById("registerToastMessage");
const registerToastClose = document.getElementById("registerToastClose");
let registerToastTimer;


// =========================
// HIỂN THỊ THÔNG BÁO
// =========================

function hideRegisterToast() {
    window.clearTimeout(registerToastTimer);
    registerToast.classList.add("hidden");
    registerToast.classList.remove("leaving");
}

function showRegisterToast(message, type = "error", autoHideMs = 5000) {
    window.clearTimeout(registerToastTimer);
    registerToast.className = "register-toast " + type;
    registerToastIcon.textContent = type === "success" ? "✓" : "!";
    registerToastTitle.textContent = type === "success" ? "Đăng ký thành công!" : "Không thể đăng ký";
    registerToastMessage.textContent = message;
    if (autoHideMs) {
        registerToastTimer = window.setTimeout(hideRegisterToast, autoHideMs);
    }
}

registerToastClose.addEventListener("click", hideRegisterToast);


// =========================
// XỬ LÝ ĐĂNG KÝ
// =========================

registerForm.addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();

        // Chặn nhấp đôi hoặc gửi lại form khi yêu cầu trước chưa hoàn tất.
        if (registerForm.dataset.submitting === "true") {
            return;
        }


        // =========================
        // LẤY DỮ LIỆU
        // =========================

        const userName =
            document
                .getElementById(
                    "registerUserName"
                )
                .value
                .trim();


        const email =
            document
                .getElementById(
                    "registerEmail"
                )
                .value
                .trim();


        const password =
            document
                .getElementById(
                    "registerPassword"
                )
                .value;


        const confirmPassword =
            document
                .getElementById(
                    "registerConfirmPassword"
                )
                .value;


        // =========================
        // VALIDATE USERNAME
        // =========================

        if (userName.length < 3) {

            showRegisterToast(
                "Tên đăng nhập cần có ít nhất 3 ký tự."
            );

            return;
        }


        // =========================
        // VALIDATE EMAIL
        // =========================

        if (email === "") {

            showRegisterToast(
                "Vui lòng nhập email."
            );

            return;
        }


        // =========================
        // VALIDATE PASSWORD
        // =========================

        if (password.length < 6) {

            showRegisterToast(
                "Mật khẩu cần có ít nhất 6 ký tự."
            );

            return;
        }


        // =========================
        // CONFIRM PASSWORD
        // =========================

        if (
            password !==
            confirmPassword
        ) {

            showRegisterToast(
                "Mật khẩu xác nhận không khớp."
            );

            return;
        }


        // =========================
        // LOADING
        // =========================

        registerButton.disabled =
            true;

        registerButton.textContent =
            "Đang tạo tài khoản…";

        registerForm.dataset.submitting = "true";


        try {

            // =========================
            // GỌI API REGISTER
            // =========================

            await apiPost(
                "/Auth/register",
                {
                    userName:
                        userName,

                    email:
                        email,

                    password:
                        password,

                    role:
                        "Student"
                }
            );


            // =========================
            // THÀNH CÔNG
            // =========================

            showRegisterToast(
                "Tài khoản học viên đã được tạo. Đang chuyển đến trang đăng nhập…",
                "success",
                1200
            );

            // Giữ trạng thái khóa để không gửi lần hai trong lúc chuyển trang.
            registerButton.textContent = "Đã tạo tài khoản";


            window.setTimeout(
                function () {

                    window.location.href =
                        "../index.html";

                },
                1400
            );

        } catch (error) {

            console.error(
                "Register error:",
                error
            );


            showRegisterToast(
                error.message ||
                "Không thể tạo tài khoản. Vui lòng thử lại."
            );

            registerButton.disabled =
                false;

            registerButton.textContent =
                "Tạo tài khoản";

            delete registerForm.dataset.submitting;
        }
    }
);
