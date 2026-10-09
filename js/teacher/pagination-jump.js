(function () {
    var requestedPage = null;
    var currentPage = 1;
    var originalApiGet = window.apiGet;

    // The dashboard owns its page value in a private closure. Override only
    // the request URL so an entered page is fetched in one request.
    window.apiGet = function (endpoint) {
        if (requestedPage !== null && typeof endpoint === "string" && /[?&]page=\d+/.test(endpoint)) {
            currentPage = requestedPage;
            endpoint = endpoint.replace(/([?&]page=)\d+/, "$1" + requestedPage);
            requestedPage = null;
        }
        return originalApiGet(endpoint);
    };

    function getPageInfo(meta) {
        var match = meta.textContent.match(/Trang\s+(\d+)\s*\/\s*(\d+)/);
        return match ? { current: Number(match[1]), total: Number(match[2]) } : null;
    }

    function requestPage(target) {
        var meta = document.getElementById("teacherMeta");
        var info = meta && getPageInfo(meta);
        if (!info || target < 1 || target > info.total || target === currentPage) return;
        requestedPage = target;
        document.getElementById("teacherRefresh")?.click();
    }

    function install() {
        var meta = document.getElementById("teacherMeta");
        if (!meta) return;
        var info = getPageInfo(meta);
        if (!info) return;

        if (info.current !== currentPage) {
            meta.dataset.jumpInstalled = "";
            meta.innerHTML = meta.innerHTML.replace(/Trang\s+\d+\s*\//, "Trang " + currentPage + " /");
            info.current = currentPage;
        }
        if (meta.dataset.jumpInstalled === "true") return;

        var controls = meta.querySelector(".pager-controls");
        var next = document.getElementById("teacherNext");
        if (!controls || !next) return;
        meta.dataset.jumpInstalled = "true";

        var label = document.createElement("label");
        label.textContent = "Đến ";
        label.style.cssText = "display:inline-flex;align-items:center;gap:5px;font-size:13px;font-weight:600;color:#475467";
        var input = document.createElement("input");
        input.type = "number";
        input.min = "1";
        input.max = String(info.total);
        input.value = String(currentPage);
        input.inputMode = "numeric";
        input.setAttribute("aria-label", "Chuyển đến trang");
        input.style.cssText = "width:56px;padding:7px;border:1px solid #d0d5dd;border-radius:8px;text-align:center;font:inherit";
        var button = document.createElement("button");
        button.type = "button";
        button.className = "pager-button";
        button.textContent = "Đi";
        label.appendChild(input);
        controls.insertBefore(label, next);
        controls.insertBefore(button, next);

        function jump() {
            var target = Number(input.value);
            if (Number.isInteger(target)) requestPage(target);
        }
        button.addEventListener("click", jump);
        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                jump();
            }
        });
    }

    document.addEventListener("click", function (event) {
        var button = event.target.closest("#teacherPrevious, #teacherNext");
        if (!button) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        requestPage(currentPage + (button.id === "teacherNext" ? 1 : -1));
    }, true);

    new MutationObserver(install).observe(document.body, { childList: true, subtree: true });
    install();
})();
