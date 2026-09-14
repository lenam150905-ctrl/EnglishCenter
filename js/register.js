// =========================
// LẤY PHẦN TỬ HTML
// =========================

const registerForm =
    document.getElementById(
        "registerForm"
    );

const registerMessage =
    document.getElementById(
        "registerMessage"
    );

const registerButton =
    document.getElementById(
        "registerButton"
    );


// =========================
// HIỂN THỊ THÔNG BÁO
// =========================

function showRegisterMessage(
    text,
    type = "error"
) {

    registerMessage.textContent =
        text;

    registerMessage.className =
        "login-message " + type;
}


// =========================
// XỬ LÝ ĐĂNG KÝ
// =========================

registerForm.addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();


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

            showRegisterMessage(
                "Tên đăng nhập cần có ít nhất 3 ký tự."
            );

            return;
        }


        // =========================
        // VALIDATE EMAIL
        // =========================

        if (email === "") {

            showRegisterMessage(
                "Vui lòng nhập email."
            );

            return;
        }


        // =========================
        // VALIDATE PASSWORD
        // =========================

        if (password.length < 6) {

            showRegisterMessage(
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

            showRegisterMessage(
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

            showRegisterMessage(
                "Tạo tài khoản thành công. Đang chuyển đến trang đăng nhập…",
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
                "Register error:",
                error
            );


            showRegisterMessage(
                error.message ||
                "Không thể tạo tài khoản. Vui lòng thử lại."
            );

        } finally {

            registerButton.disabled =
                false;

            registerButton.textContent =
                "Tạo tài khoản";
        }
    }
);