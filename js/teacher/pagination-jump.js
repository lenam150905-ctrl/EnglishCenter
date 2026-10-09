(function () {
    function getPageInfo(meta) {
        var match = meta.textContent.match(/Trang\s+(\d+)\s*\/\s*(\d+)/);
        return match ? { current: Number(match[1]), total: Number(match[2]) } : null;
    }

    function install() {
        var meta = document.getElementById("teacherMeta");
        if (!meta || meta.dataset.jumpInstalled === "true") return;

        var controls = meta.querySelector(".pager-controls");
        var next = document.getElementById("teacherNext");
        var info = getPageInfo(meta);
        if (!controls || !next || !info) return;

        meta.dataset.jumpInstalled = "true";
        var label = document.createElement("label");
        label.textContent = "Đến ";
        label.style.cssText = "display:inline-flex;align-items:center;gap:5px;font-size:13px;font-weight:600;color:#475467";
        var input = document.createElement("input");
        input.type = "number";
        input.min = "1";
        input.max = String(info.total);
        input.value = String(info.current);
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

        function move(target) {
            var latestMeta = document.getElementById("teacherMeta");
            var latest = latestMeta && getPageInfo(latestMeta);
            if (!latest || latest.current === target) return;
            var direction = target > latest.current ? "teacherNext" : "teacherPrevious";
            var navigation = document.getElementById(direction);
            if (!navigation || navigation.disabled) return;
            var observer = new MutationObserver(function () {
                observer.disconnect();
                window.setTimeout(function () { move(target); }, 0);
            });
            observer.observe(latestMeta, { childList: true, subtree: true });
            navigation.click();
        }

        function jump() {
            var latest = getPageInfo(document.getElementById("teacherMeta"));
            var target = Number(input.value);
            if (!latest || !Number.isInteger(target) || target < 1 || target > latest.total) return;
            move(target);
        }

        button.addEventListener("click", jump);
        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                jump();
            }
        });
    }

    new MutationObserver(install).observe(document.body, { childList: true, subtree: true });
    install();
})();
