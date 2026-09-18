(function () {
    var query = new URLSearchParams(location.search);
    var success = query.get("status") === "success";
    var title = document.getElementById("paymentTitle");
    var message = document.getElementById("paymentMessage");
    var invoice = query.get("invoiceId");
    var role = typeof getCurrentRole === "function" ? getCurrentRole() : "Student";
    var destinations = {
        Admin: "../admin/dashboard.html#invoices",
        Teacher: "../teacher/dashboard.html#invoices",
        Student: "../student/dashboard.html#invoices"
    };
    document.getElementById("paymentBack").href = destinations[role] || "../../index.html";
    document.getElementById("paymentBack").textContent = role === "Admin" ? "Về quản lý hóa đơn" : "Về hóa đơn của tôi";
    document.getElementById("paymentCard").classList.toggle("failed", !success);
    document.getElementById("paymentIcon").textContent = success ? "✓" : "!";
    title.textContent = success ? "Thanh toán thành công" : "Thanh toán chưa thành công";
    message.textContent = query.get("message") || (success ? "Hóa đơn của bạn đã được cập nhật." : "Giao dịch chưa hoàn tất. Bạn có thể thử lại từ trang hóa đơn.");
    document.getElementById("invoiceReference").textContent = invoice ? "Hóa đơn #" + invoice : "";
})();
