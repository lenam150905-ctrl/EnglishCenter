(function () {
    if (!requireRole("Student")) return;
    var content = document.getElementById("studentContent");
    var name = getCurrentUserName() || "Học viên";
    var studentId = localStorage.getItem("studentProfileId") || sessionStorage.getItem("studentProfileId") || "";
    var views = {
        courses: { title: "Khóa học", endpoint: "/Courses", columns: [["courseCode", "Mã khóa"], ["courseName", "Khóa học"], ["duration", "Thời lượng"], ["tuitionFee", "Học phí"], ["status", "Trạng thái"]] },
        schedules: { title: "Lịch học", endpoint: "/Schedules", columns: [["courseName", "Khóa học"], ["teacherName", "Giáo viên"], ["startTime", "Bắt đầu"], ["endTime", "Kết thúc"], ["room", "Phòng"]] },
        exams: { title: "Bài kiểm tra", endpoint: "/Exams", columns: [["examName", "Tên bài kiểm tra"], ["examType", "Loại"], ["courseName", "Khóa học"], ["examDate", "Ngày thi"]] },
        grades: { title: "Điểm số của tôi", endpoint: "/Grades/mine", columns: [["examName", "Bài kiểm tra"], ["score", "Điểm"], ["comment", "Nhận xét"]] },
        certificates: { title: "Chứng chỉ", endpoint: "/Certificates/mine", columns: [["certificateCode", "Mã chứng chỉ"], ["studentName", "Học viên"], ["courseName", "Khóa học"], ["issueDate", "Ngày cấp"]] },
        invoices: { title: "Hóa đơn & thanh toán", endpoint: "/Invoices/mine", columns: [["id", "Mã hóa đơn"], ["courseName", "Khóa học"], ["amount", "Số tiền"], ["invoiceDate", "Ngày lập"], ["status", "Trạng thái"]] },
        notifications: { title: "Thông báo", endpoint: "/Notifications", columns: [["title", "Tiêu đề"], ["message", "Nội dung"], ["type", "Loại"], ["isRead", "Trạng thái"], ["createdAt", "Thời gian"]] }
    };
    function escapeHtml(data) {
        return String(data === null || data === undefined ? "—" : data).replace(/[&<>'"]/g, function (char) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char];
        });
    }
    function route() { return location.hash.slice(1) || "dashboard"; }
    function updateUser() {
        var letter = name.trim().charAt(0).toUpperCase() || "H";
        ["studentSidebarName", "studentHeaderName"].forEach(function (id) { document.getElementById(id).textContent = name; });
        ["studentSidebarAvatar", "studentHeaderAvatar"].forEach(function (id) { document.getElementById(id).textContent = letter; });
    }
    function setupShell() {
        updateUser();
        document.getElementById("studentLogoutButton").onclick = logout;
        document.getElementById("studentMenuButton").onclick = function () {
            document.getElementById("studentSidebar").classList.toggle("open");
            document.getElementById("studentOverlay").classList.toggle("show");
        };
        document.getElementById("studentOverlay").onclick = closeSidebar;
    }
    function closeSidebar() {
        document.getElementById("studentSidebar").classList.remove("open");
        document.getElementById("studentOverlay").classList.remove("show");
    }
    function setHeader(title, description) {
        document.getElementById("studentPageTitle").textContent = title;
        document.getElementById("studentPageDescription").textContent = description;
    }
    function activate(current) {
        document.querySelectorAll("[data-student-route]").forEach(function (link) { link.classList.toggle("active", link.dataset.studentRoute === current); });
    }
    function display(key, data) {
        if (data === null || data === undefined || data === "") return "—";
        if (/date|time/i.test(key)) return new Date(data).toLocaleString("vi-VN");
        if (key === "tuitionFee" || key === "amount") return Number(data).toLocaleString("vi-VN") + " đ";
        if (key === "status" || key === "isRead") return '<span class="status-badge">' + (key === "isRead" ? (data ? "Đã đọc" : "Chưa đọc") : escapeHtml(data)) + "</span>";
        return escapeHtml(data);
    }
    function tablePage(name) {
        var view = views[name];
        setHeader(view.title, "Theo dõi " + view.title.toLowerCase() + " của bạn");
        content.innerHTML = '<section class="admin-page"><div class="section-header"><div><h2>' + view.title + "</h2><p>Thông tin học tập được cập nhật từ hệ thống.</p></div><button class=\"refresh-button\" id=\"studentRefresh\">↻ Làm mới</button></div><div class=\"page-toolbar\"><label class=\"search-box\">⌕<input id=\"studentSearch\" placeholder=\"Tìm kiếm...\" autocomplete=\"off\"></label></div><div class=\"table-panel\"><div class=\"data-table-wrap\"><table class=\"data-table\"><thead><tr>" + view.columns.map(function (column) { return "<th>" + column[1] + "</th>"; }).join("") + (name === "courses" ? "<th>Đăng ký</th>" : name === "exams" ? "<th>Thao tác</th>" : name === "certificates" ? "<th>Thao tác</th>" : name === "invoices" ? "<th>Thanh toán</th>" : name === "notifications" ? "<th>Thao tác</th>" : "") + "</tr></thead><tbody id=\"studentTable\"></tbody></table></div><div class=\"page-meta\" id=\"studentMeta\"></div></div></section><div id=\"studentModal\"></div>";
        if (name === "notifications") {
            var readAllButton = document.createElement("button");
            readAllButton.type = "button";
            readAllButton.className = "secondary-button";
            readAllButton.textContent = "✓ Đã đọc tất cả";
            var refreshButton = document.getElementById("studentRefresh");
            var actions = document.createElement("div");
            actions.className = "page-actions";
            refreshButton.parentNode.insertBefore(actions, refreshButton);
            actions.append(readAllButton, refreshButton);
            readAllButton.onclick = function () { readAll(reload); };
        }
        var timer, page = 1;
        function reload() { loadTable(name, page, document.getElementById("studentSearch").value.trim(), reload, function (nextPage) { page = nextPage; reload(); }); }
        document.getElementById("studentRefresh").onclick = reload;
        document.getElementById("studentSearch").oninput = function () { clearTimeout(timer); timer = setTimeout(reload, 250); };
        reload();
    }
    async function loadTable(name, page, search, reload, changePage) {
        var view = views[name], table = document.getElementById("studentTable"), meta = document.getElementById("studentMeta");
        table.innerHTML = '<tr><td colspan="' + (view.columns.length + 1) + '" class="table-empty">Đang tải dữ liệu…</td></tr>';
        try {
            var endpoint = view.endpoint === "/Notifications" ? view.endpoint : view.endpoint + "?page=" + page + "&pageSize=20&search=" + encodeURIComponent(search);
            var response = await apiGet(endpoint);
            var rows = Array.isArray(response) ? response : (response.data || []);
            var total = Array.isArray(response) ? rows.length : (response.totalItems || rows.length);
            var totalPages = Array.isArray(response) ? Math.max(1, Math.ceil(total / 20)) : (response.totalPages || 1);
            if (Array.isArray(response)) {
                rows = rows.slice((page - 1) * 20, page * 20);
            }
            table.innerHTML = rows.length ? rows.map(function (row) {
                var cells = view.columns.map(function (column) { return "<td>" + display(column[0], row[column[0]]) + "</td>"; }).join("");
                if (name === "courses") cells += '<td><button class="table-button" data-enroll="' + row.id + '">Đăng ký</button></td>';
                if (name === "exams") cells += '<td><button class="table-button" data-exam="' + row.id + '">Kiểm tra</button></td>';
                if (name === "certificates") cells += '<td><button class="table-button" data-pdf="' + row.id + '">Xuất PDF</button></td>';
                if (name === "invoices") cells += '<td>' + (row.status === "Unpaid" ? '<button class="table-button" data-pay="' + row.id + '">Thanh toán VNPay</button>' : '—') + '</td>';
                if (name === "notifications") cells += '<td><div class="row-actions"><button class="table-button" data-read="' + row.id + '"' + (row.isRead ? ' disabled' : '') + '>Đã đọc</button><button class="table-button danger" data-delete-notification="' + row.id + '">Xóa</button></div></td>';
                return "<tr>" + cells + "</tr>";
            }).join("") : '<tr><td colspan="' + (view.columns.length + 1) + '" class="table-empty">Chưa có dữ liệu.</td></tr>';
            meta.innerHTML = 'Hiển thị ' + rows.length + ' / ' + total + ' bản ghi <span class="pager-controls"><button class="pager-button" id="studentPrevious"' + (page <= 1 ? ' disabled' : '') + '>← Trước</button><span>Trang ' + page + ' / ' + totalPages + '</span><button class="pager-button" id="studentNext"' + (page >= totalPages ? ' disabled' : '') + '>Sau →</button></span>';
            table.querySelectorAll("[data-enroll]").forEach(function (button) { button.onclick = function () { enroll(button.dataset.enroll, reload); }; });
            table.querySelectorAll("[data-exam]").forEach(function (button) { button.onclick = function () { checkExam(button.dataset.exam); }; });
            table.querySelectorAll("[data-pdf]").forEach(function (button) { button.onclick = function () { exportPdf(button.dataset.pdf); }; });
            table.querySelectorAll("[data-pay]").forEach(function (button) { button.onclick = function () { payInvoice(button.dataset.pay); }; });
            table.querySelectorAll("[data-read]").forEach(function (button) { button.onclick = function () { markRead(button.dataset.read, reload); }; });
            table.querySelectorAll("[data-delete-notification]").forEach(function (button) { button.onclick = function () { deleteNotification(button.dataset.deleteNotification, reload); }; });
            document.getElementById("studentPrevious")?.addEventListener("click", function () { changePage(page - 1); });
            document.getElementById("studentNext")?.addEventListener("click", function () { changePage(page + 1); });
        } catch (error) { table.innerHTML = '<tr><td colspan="' + (view.columns.length + 1) + '" class="table-empty table-error">' + escapeHtml(error.message) + "</td></tr>"; }
    }
    async function enroll(courseId, reload) {
        try {
            await apiPost("/Enrollments", { courseId: Number(courseId), enrollmentDate: new Date().toISOString(), status: "Pending" });
            alert("Đăng ký đã được gửi. Trung tâm sẽ xác nhận sớm."); reload();
        } catch (error) { alert(error.message); }
    }
    async function checkExam(examId) {
        try { var result = await apiGet("/Exams/" + examId + "/can-start"); alert(result.canStart ? "Bạn đủ điều kiện bắt đầu bài kiểm tra." : (result.message || "Bạn chưa đủ điều kiện.")); } catch (error) { alert(error.message); }
    }
    async function exportPdf(id) { try { await apiDownload("/Certificates/" + id + "/download", "chung-chi-" + id + ".pdf"); alert("Đã tải chứng chỉ PDF."); } catch (error) { alert(error.message); } }
    async function payInvoice(id) { try { var result = await apiPost("/Payments/create-vnpay/" + id, {}); var url = result && (result.paymentUrl || result.url); if (!url) throw new Error("Không nhận được liên kết thanh toán."); window.location.assign(url); } catch (error) { alert(error.message); } }
    async function markRead(id, reload) { try { await apiPut("/Notifications/" + id + "/read", {}); reload(); } catch (error) { alert(error.message); } }
    async function readAll(reload) { try { await apiPut("/Notifications/read-all", {}); reload(); } catch (error) { alert(error.message); } }
    async function deleteNotification(id, reload) { if (!(await appConfirm("Bạn có chắc muốn xóa thông báo này?", "Xóa thông báo"))) return; try { await apiDelete("/Notifications/" + id); reload(); } catch (error) { alert(error.message); } }
    async function profilePage() {
        setHeader("Hồ sơ của tôi", "Cập nhật thông tin dùng cho các thao tác học tập");
        content.innerHTML = '<section class="admin-page student-form"><div class="section-header"><div><h2>Hồ sơ học viên</h2><p>Đang tải thông tin hồ sơ…</p></div></div></section>';
        try {
            var profile = await apiGet("/Students/me");
            studentId = profile.id;
            content.innerHTML = '<section class="admin-page student-form"><div class="section-header"><div><h2>Hồ sơ học viên</h2><p>Mã học viên được hệ thống gắn theo tài khoản đăng nhập.</p></div></div><div class="table-panel"><div class="modal-card"><div class="form-grid"><label>Tên đăng nhập<input value="' + escapeHtml(name) + '" disabled></label><label>Mã học viên<input value="' + escapeHtml(profile.id) + '" disabled></label><label>Họ và tên<input value="' + escapeHtml(profile.fullName) + '" disabled></label><label>Email<input value="' + escapeHtml(profile.email) + '" disabled></label></div><p class="student-note">Bạn không cần nhập mã học viên. Khi đăng ký khóa học, hệ thống tự dùng đúng hồ sơ của bạn.</p></div></div></section>';
        } catch (error) { content.innerHTML = '<section class="admin-page"><p class="table-empty table-error">' + escapeHtml(error.message) + '</p></section>'; }
    }
    async function dashboardPage() {
        setHeader("Trang chủ", "Không gian học tập của bạn");
        content.innerHTML = '<section class="student-hero"><span class="eyebrow">ENGLISH CENTER</span><h2>Chào ' + escapeHtml(name) + " 👋</h2><p>Theo dõi khóa học, lịch học và các thông báo mới nhất tại một nơi.</p></section><section class=\"student-card-grid\"><article class=\"student-stat\"><span>Khóa học đang mở</span><strong id=\"courseTotal\">…</strong></article><article class=\"student-stat\"><span>Bài kiểm tra</span><strong id=\"examTotal\">…</strong></article><article class=\"student-stat\"><span>Thông báo</span><strong id=\"notificationTotal\">…</strong></article></section><section class=\"table-panel\"><div class=\"panel-header\"><div><h3>Truy cập nhanh</h3><p>Tiếp tục hành trình học tập của bạn.</p></div></div><div class=\"quick-grid\"><a href=\"#courses\" data-student-route=\"courses\" class=\"quick-item\"><div class=\"quick-icon\">📚</div><div><strong>Khóa học</strong><span>Xem và đăng ký khóa học</span></div></a><a href=\"#schedules\" data-student-route=\"schedules\" class=\"quick-item\"><div class=\"quick-icon\">📅</div><div><strong>Lịch học</strong><span>Xem lịch học sắp tới</span></div></a><a href=\"#notifications\" data-student-route=\"notifications\" class=\"quick-item\"><div class=\"quick-icon\">🔔</div><div><strong>Thông báo</strong><span>Xem tin nhắn mới</span></div></a></div></section>";
        try {
            var results = await Promise.all([apiGet("/Courses?page=1&pageSize=1"), apiGet("/Exams?page=1&pageSize=1"), apiGet("/Notifications")]);
            document.getElementById("courseTotal").textContent = results[0].totalItems || 0;
            document.getElementById("examTotal").textContent = results[1].totalItems || 0;
            document.getElementById("notificationTotal").textContent = Array.isArray(results[2]) ? results[2].filter(function (item) { return !item.isRead; }).length : 0;
        } catch (_) { ["courseTotal", "examTotal", "notificationTotal"].forEach(function (id) { document.getElementById(id).textContent = "—"; }); }
    }
    function render(current) {
        var safe = views[current] || ["dashboard", "profile"].includes(current) ? current : "dashboard";
        activate(safe); closeSidebar();
        if (safe === "dashboard") dashboardPage(); else if (safe === "profile") profilePage(); else tablePage(safe);
    }
    document.addEventListener("click", function (event) {
        var link = event.target.closest("[data-student-route]"); if (!link) return;
        event.preventDefault(); if (route() === link.dataset.studentRoute) render(route()); else location.hash = link.dataset.studentRoute;
    });
    window.addEventListener("hashchange", function () { render(route()); });
    setupShell(); render(route());
})();
