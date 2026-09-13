const API_BASE_URL = "https://localhost:7207/api";
const registerForm = document.getElementById("registerForm");
const registerMessage = document.getElementById("registerMessage");
const registerButton = document.getElementById("registerButton");
function showRegisterMessage(text, type = "error") {
    registerMessage.textContent = text;
    registerMessage.className = "login-message " + type;
}
registerForm.addEventListener("submit", async event => {
    event.preventDefault();
    const userName = document.getElementById("registerUserName").value.trim();
    const email = document.getElementById("registerEmail").value.trim();
    const password = document.getElementById("registerPassword").value;
    const confirmPassword = document.getElementById("registerConfirmPassword").value;
    if (userName.length < 3) { showRegisterMessage("Tên đăng nhập cần có ít nhất 3 ký tự."); return; }
    if (password.length < 6) { showRegisterMessage("Mật khẩu cần có ít nhất 6 ký tự."); return; }
    if (password !== confirmPassword) { showRegisterMessage("Mật khẩu xác nhận không khớp."); return; }
    registerButton.disabled = true;
    registerButton.textContent = "Đang tạo tài khoản…";
    try {
        const response = await fetch(API_BASE_URL + "/Auth/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userName, email, password, role: "Student" })
        });
        const rawResponse = await response.text();
        let data = {};
        try {
            data = JSON.parse(rawResponse);
        } catch {
            data.message = rawResponse;
        }
        if (!response.ok) {
            throw new Error(
                data.message || "Không thể tạo tài khoản. Vui lòng thử lại."
            );
        }
        showRegisterMessage("Tạo tài khoản thành công. Đang chuyển đến trang đăng nhập…", "success");
        window.setTimeout(() => { window.location.href = "../index.html"; }, 1200);
    } catch (error) { showRegisterMessage(error.message); }
    finally {
        registerButton.disabled = false;
        registerButton.textContent = "Tạo tài khoản";
    }
});
