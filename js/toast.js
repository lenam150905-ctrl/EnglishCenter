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
    window.appConfirm = function (message, title) {
        return new Promise(function (resolve) {
            var modal = document.createElement("div");
            modal.className = "app-confirm-backdrop";
            modal.innerHTML = '<div class="app-confirm" role="dialog" aria-modal="true"><div class="confirm-icon">!</div><h3></h3><p></p><div><button type="button" class="secondary-button" data-cancel>Hủy</button><button type="button" class="table-button danger" data-accept>Xác nhận</button></div></div>';
            modal.querySelector("h3").textContent = title || "Xác nhận thao tác";
            modal.querySelector("p").textContent = message || "Bạn có chắc muốn tiếp tục?";
            function close(value) { modal.remove(); resolve(value); }
            modal.querySelector("[data-cancel]").onclick = function () { close(false); };
            modal.querySelector("[data-accept]").onclick = function () { close(true); };
            modal.onclick = function (event) { if (event.target === modal) close(false); };
            document.body.appendChild(modal);
        });
    };
})();
