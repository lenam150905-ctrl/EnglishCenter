(function () {
    function showToast(message, type) {
        var host = document.getElementById("toastHost");
        if (!host) {
            host = document.createElement("div");
            host.id = "toastHost";
            host.className = "toast-host";
            document.body.appendChild(host);
        }
        var toast = document.createElement("div");
        toast.className = "app-toast " + (type || "info");
        toast.innerHTML = '<span class="toast-icon">' + (type === "error" ? "!" : "✓") + '</span><span class="toast-message"></span><button class="toast-close" type="button" aria-label="Đóng">×</button>';
        toast.querySelector(".toast-message").textContent = String(message || "Đã hoàn tất.");
        toast.querySelector(".toast-close").onclick = function () { toast.remove(); };
        host.appendChild(toast);
        window.setTimeout(function () { toast.classList.add("leaving"); window.setTimeout(function () { toast.remove(); }, 180); }, 3600);
    }
    window.showToast = showToast;
    window.alert = function (message) { showToast(message, /lỗi|không thể|không có quyền|hết hạn/i.test(String(message)) ? "error" : "success"); };
})();
