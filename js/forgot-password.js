const API_BASE_URL = "https://localhost:7207/api";
const forms = [document.getElementById("forgotForm"), document.getElementById("otpForm"), document.getElementById("resetForm")];
const steps = document.querySelectorAll(".progress-steps span");
const title = document.getElementById("recoveryTitle");
const description = document.getElementById("recoveryDescription");
const message = document.getElementById("recoveryMessage");
const emailInput = document.getElementById("recoveryEmail");
const otpInput = document.getElementById("recoveryOtp");
const newPasswordInput = document.getElementById("newPassword");
const confirmPasswordInput = document.getElementById("confirmPassword");
let recoveryEmail = "";
let recoveryOtp = "";
const copy = [["Quên mật khẩu?", "Nhập email để nhận mã OTP gồm 6 chữ số."], ["Kiểm tra email", "Nhập mã OTP vừa được gửi đến email của bạn."], ["Tạo mật khẩu mới", "Chọn mật khẩu mới có ít nhất 6 ký tự."]];

function showMessage(text, type = "error") {
    message.textContent = text;
    message.className = "login-message " + type;
}
function setStep(step) {
    forms.forEach((form, index) => form.classList.toggle("active", index === step));
    steps.forEach((item, index) => item.classList.toggle("active", index <= step));
    [title.textContent, description.textContent] = copy[step];
    showMessage("", "");
    if (step === 1) otpInput.focus();
    if (step === 2) newPasswordInput.focus();
}
async function request(path, body) {
    const response = await fetch(API_BASE_URL + "/Auth/" + path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(data.message || "Không thể xử lý yêu cầu. Vui lòng thử lại.");
    return data;
}
otpInput.addEventListener("input", () => { otpInput.value = otpInput.value.replace(/\D/g, "").slice(0, 6); });
forms[0].addEventListener("submit", async event => {
    event.preventDefault();
    recoveryEmail = emailInput.value.trim();
    try {
        await request("forgot-password", { email: recoveryEmail });
        setStep(1);
        showMessage("Mã OTP đã được gửi. Mã có hiệu lực trong 5 phút.", "success");
    } catch (error) { showMessage(error.message); }
});
forms[1].addEventListener("submit", async event => {
    event.preventDefault();
    recoveryOtp = otpInput.value.trim();
    if (!/^\d{6}$/.test(recoveryOtp)) { showMessage("Mã OTP phải gồm đúng 6 chữ số."); return; }
    try {
        await request("verify-otp", { email: recoveryEmail, otp: recoveryOtp });
        setStep(2);
    } catch (error) { showMessage(error.message); }
});
forms[2].addEventListener("submit", async event => {
    event.preventDefault();
    const newPassword = newPasswordInput.value;
    if (newPassword.length < 6) { showMessage("Mật khẩu mới phải có ít nhất 6 ký tự."); return; }
    if (newPassword !== confirmPasswordInput.value) { showMessage("Mật khẩu xác nhận không khớp."); return; }
    try {
        await request("reset-password", { email: recoveryEmail, otp: recoveryOtp, newPassword, confirmPassword: confirmPasswordInput.value });
        showMessage("Đặt lại mật khẩu thành công. Đang chuyển đến trang đăng nhập…", "success");
        window.setTimeout(() => { window.location.href = "../index.html"; }, 1200);
    } catch (error) { showMessage(error.message); }
});
