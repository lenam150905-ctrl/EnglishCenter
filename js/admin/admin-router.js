/* Admin SPA router. Header and sidebar are kept mounted once. */
(function () {
    var content = document.getElementById("adminContent");
    var dashboardMarkup = content ? content.innerHTML : "";
    var headerTitle = document.querySelector(".header-left h1");
    var headerDescription = document.querySelector(".header-left p");
    var views = {
        users: makeView("Người dùng", "/Users", [["userName", "Tên đăng nhập"], ["email", "Email"], ["role", "Vai trò"]], [["userName", "Tên đăng nhập", "text"], ["email", "Email", "email"], ["password", "Mật khẩu", "password"], ["role", "Vai trò", "select", "Admin,Teacher,Student"]]),
        students: makeView("Học viên", "/Students", [["fullName", "Họ và tên"], ["email", "Email"], ["phone", "Điện thoại"], ["dateOfBirth", "Ngày sinh"]], [["fullName", "Họ và tên", "text"], ["dateOfBirth", "Ngày sinh", "date"], ["email", "Email", "email"], ["phone", "Điện thoại", "text"], ["address", "Địa chỉ", "textarea"], ["userId", "Tài khoản học viên", "lookup", "studentUsers"]], { importable: true }),
        teachers: makeView("Giáo viên", "/Teachers", [["fullName", "Họ và tên"], ["email", "Email"], ["phone", "Điện thoại"], ["specialization", "Chuyên môn"]], [["fullName", "Họ và tên", "text"], ["email", "Email", "email"], ["phone", "Điện thoại", "text"], ["specialization", "Chuyên môn", "text"], ["userId", "Tài khoản giáo viên", "lookup", "teacherUsers"]]),
        courses: makeView("Khóa học", "/Courses", [["courseCode", "Mã khóa"], ["courseName", "Tên khóa học"], ["duration", "Thời lượng"], ["tuitionFee", "Học phí"], ["status", "Trạng thái"]], [["courseCode", "Mã khóa học", "text"], ["courseName", "Tên khóa học", "text"], ["duration", "Thời lượng (buổi)", "number"], ["tuitionFee", "Học phí", "number"], ["description", "Mô tả", "textarea"], ["status", "Trạng thái", "select", "Pending,Active,Closed"]]),
        schedules: makeView("Lịch học", "/Schedules", [["courseName", "Khóa học"], ["teacherName", "Giáo viên"], ["startTime", "Bắt đầu"], ["endTime", "Kết thúc"], ["room", "Phòng"]], [["courseId", "Khóa học", "lookup", "courses"], ["teacherId", "Giáo viên", "lookup", "teachers"], ["startTime", "Bắt đầu", "datetime-local"], ["endTime", "Kết thúc", "datetime-local"], ["room", "Phòng học", "text"]]),
        enrollments: makeView("Ghi danh", "/Enrollments", [["studentName", "Học viên"], ["courseName", "Khóa học"], ["enrollmentDate", "Ngày ghi danh"], ["status", "Trạng thái"]], [["studentId", "Học viên", "lookup", "students"], ["courseId", "Khóa học", "lookup", "courses"], ["enrollmentDate", "Ngày ghi danh", "date"], ["status", "Trạng thái", "select", "Pending,Active,Cancelled"]], { cancelable: true }),
        exams: makeView("Bài kiểm tra", "/Exams", [["examName", "Tên bài kiểm tra"], ["examType", "Loại"], ["courseName", "Khóa học"], ["examDate", "Ngày thi"]], [["examName", "Tên bài kiểm tra", "text"], ["examType", "Loại bài kiểm tra", "text"], ["examDate", "Ngày thi", "datetime-local"], ["courseId", "Khóa học (nếu có)", "lookup", "courses"]]),
        grades: makeView("Điểm số", "/Grades", [["studentName", "Học viên"], ["examName", "Bài kiểm tra"], ["score", "Điểm"], ["comment", "Nhận xét"]], [["studentId", "Học viên", "lookup", "students"], ["examId", "Bài kiểm tra", "lookup", "exams"], ["score", "Điểm (0–10)", "number"], ["comment", "Nhận xét", "textarea"]]),
        invoices: makeView("Hóa đơn", "/Invoices", [["studentName", "Học viên"], ["courseName", "Khóa học"], ["amount", "Số tiền"], ["invoiceDate", "Ngày lập"], ["status", "Trạng thái"]], [["studentId", "Học viên", "lookup", "students"], ["enrollmentId", "Ghi danh (nếu có)", "lookup", "enrollments"], ["amount", "Số tiền", "number"], ["invoiceDate", "Ngày lập", "date"], ["status", "Trạng thái", "select", "Unpaid,Paid,Cancelled"]], { cancelable: true, payment: true }),
        certificates: makeView("Chứng chỉ", "/Certificates", [["certificateCode", "Mã chứng chỉ"], ["studentName", "Học viên"], ["courseName", "Khóa học"], ["issueDate", "Ngày cấp"]], [["studentId", "Học viên", "lookup", "students"], ["courseId", "Khóa học", "lookup", "courses"], ["certificateCode", "Mã chứng chỉ", "text"], ["issueDate", "Ngày cấp", "date"], ["pdfFilePath", "Đường dẫn PDF", "text"]], { exportPdf: true }),
        "audit-logs": makeView("Nhật ký hệ thống", "/AuditLog", [["createdAt", "Thời gian"], ["action", "Hành động"], ["entityName", "Đối tượng"], ["description", "Mô tả"], ["ipAddress", "Địa chỉ IP"]], [], { readonly: true }),
        notifications: makeView("Thông báo", "/Notifications", [["title", "Tiêu đề"], ["message", "Nội dung"], ["type", "Loại"], ["isRead", "Trạng thái"], ["createdAt", "Thời gian"]], [], { readonly: true, notification: true }),
        trash: makeView("Thùng rác", "/Trash", [["entity", "Loại dữ liệu"], ["id", "Mã"], ["label", "Thông tin"]], [], { readonly: true, trash: true })
    };
    function makeView(title, endpoint, columns, fields, options) {
        options = options || {};
        options.title = title; options.endpoint = endpoint; options.columns = columns; options.fields = fields;
        return options;
    }
    function route() { return location.hash.slice(1) || "dashboard"; }
    function esc(value) {
        return String(value === null || value === undefined ? "—" : value).replace(/[&<>'"]/g, function (char) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char];
        });
    }
    function value(key, data) {
        if (data === null || data === undefined || data === "") return "—";
        if (/date|time/i.test(key)) return new Date(data).toLocaleString("vi-VN");
        if (key === "amount" || key === "tuitionFee") return Number(data).toLocaleString("vi-VN") + " đ";
        if (key === "status" || key === "role" || key === "isRead") {
            var text = key === "isRead" ? (data ? "Đã đọc" : "Chưa đọc") : data;
            return '<span class="status-badge">' + esc(text) + "</span>";
        }
        return esc(data);
    }
    function activate(name) {
        document.querySelectorAll("[data-route]").forEach(function (item) {
            item.classList.toggle("active", item.dataset.route === name);
        });
    }
    function updateHeader(view) {
        if (headerTitle) headerTitle.textContent = view.title;
        if (headerDescription) headerDescription.textContent = "Quản lý " + view.title.toLowerCase() + " của English Center";
    }
    function closeSidebar() {
        document.getElementById("sidebar")?.classList.remove("open");
        document.getElementById("sidebarOverlay")?.classList.remove("show");
    }
    function listMarkup(view) {
        var headings = view.columns.map(function (column) { return "<th>" + column[1] + "</th>"; }).join("");
        if (!view.readonly || view.notification || view.trash) headings += "<th>Thao tác</th>";
        return '<section class="admin-page"><div class="section-header"><div><h2>' + view.title + "</h2><p>Quản lý dữ liệu " + view.title.toLowerCase() + '</p></div><div class="page-actions">' + (view.readonly ? "" : '<button class="primary-button" id="addRecord">+ Thêm mới</button>') + (view.notification ? '<button class="secondary-button" id="readAllNotifications">✓ Đã đọc tất cả</button>' : '') + '<button class="refresh-button" id="adminRefresh">↻ Làm mới</button></div></div><div class="page-toolbar"><label class="search-box">⌕<input id="adminSearch" placeholder="Tìm kiếm..." autocomplete="off"></label>' + (view.importable ? '<label class="secondary-button">⇧ Nhập Excel<input id="excelImport" type="file" accept=".xlsx,.xls" hidden></label>' : "") + '</div><div class="table-panel"><div class="data-table-wrap"><table class="data-table"><thead><tr>' + headings + '</tr></thead><tbody id="adminTableBody"></tbody></table></div><div class="page-meta" id="adminMeta"></div></div></section><div id="adminModal"></div>';
    }
    function renderList(name) {
        var view = views[name], page = 1, search = "", wait;
        updateHeader(view); content.innerHTML = listMarkup(view);
        function reload() { loadList(name, page, search, reload, function (nextPage) { page = nextPage; reload(); }); }
        document.getElementById("adminRefresh").onclick = reload;
        document.getElementById("addRecord")?.addEventListener("click", function () { openForm(name, null, reload); });
        document.getElementById("adminSearch").addEventListener("input", function (event) {
            clearTimeout(wait); wait = setTimeout(function () { page = 1; search = event.target.value.trim(); reload(); }, 250);
        });
        document.getElementById("excelImport")?.addEventListener("change", function (event) { importStudents(event.target.files[0], reload); });
        document.getElementById("readAllNotifications")?.addEventListener("click", function () { readAllNotifications(reload); });
        reload();
    }
    async function loadList(name, page, search, reload, changePage) {
        var view = views[name], body = document.getElementById("adminTableBody"), meta = document.getElementById("adminMeta");
        if (!body) return;
        body.innerHTML = '<tr><td colspan="' + (view.columns.length + 1) + '" class="table-empty">Đang tải dữ liệu…</td></tr>';
        try {
            var endpoint = view.readonly ? view.endpoint : view.endpoint + "?page=" + page + "&pageSize=20&search=" + encodeURIComponent(search);
            var response = await apiGet(endpoint);
            var rows = Array.isArray(response) ? response : (response.data || []);
            var total = Array.isArray(response) ? rows.length : (response.totalItems || rows.length);
            var totalPages = Array.isArray(response) ? Math.max(1, Math.ceil(total / 20)) : (response.totalPages || 1);
            if (Array.isArray(response)) {
                rows = rows.slice((page - 1) * 20, page * 20);
            }
            body.innerHTML = rows.length ? rows.map(function (row) {
                var cells = view.columns.map(function (column) { return "<td>" + value(column[0], row[column[0]]) + "</td>"; }).join("");
                if (!view.readonly) cells += '<td><div class="row-actions"><button class="table-button" data-edit="' + row.id + '">Sửa</button>' + (view.cancelable ? '<button class="table-button warning" data-cancel="' + row.id + '">Hủy</button>' : "") + (view.payment ? '<button class="table-button" data-pay="' + row.id + '">VNPay</button>' : "") + (view.exportPdf ? '<button class="table-button" data-pdf="' + row.id + '">PDF</button>' : "") + '<button class="table-button danger" data-delete="' + row.id + '">Xóa</button></div></td>';
                else if (view.notification) cells += '<td><div class="row-actions"><button class="table-button" data-read="' + row.id + '"' + (row.isRead ? " disabled" : "") + '>Đã đọc</button><button class="table-button danger" data-delete-notification="' + row.id + '">Xóa</button></div></td>';
                else if (view.trash) cells += '<td><button class="table-button" data-restore-entity="' + esc(row.entity) + '" data-restore-id="' + row.id + '">Khôi phục</button></td>';
                return "<tr>" + cells + "</tr>";
            }).join("") : '<tr><td colspan="' + (view.columns.length + 1) + '" class="table-empty">Chưa có dữ liệu phù hợp.</td></tr>';
            meta.innerHTML = 'Hiển thị ' + rows.length + ' / ' + total + ' bản ghi <span class="pager-controls"><button class="pager-button" id="previousPage"' + (page <= 1 ? ' disabled' : '') + '>← Trước</button><span>Trang ' + page + ' / ' + totalPages + '</span><button class="pager-button" id="nextPage"' + (page >= totalPages ? ' disabled' : '') + '>Sau →</button></span>';
            body.querySelectorAll("[data-edit]").forEach(function (button) { button.onclick = function () { openForm(name, rows.find(function (row) { return row.id === Number(button.dataset.edit); }), reload); }; });
            body.querySelectorAll("[data-delete]").forEach(function (button) { button.onclick = function () { remove(view, button.dataset.delete, reload); }; });
            body.querySelectorAll("[data-cancel]").forEach(function (button) { button.onclick = function () { action(view.endpoint, button.dataset.cancel, "cancel", "PUT", reload); }; });
            body.querySelectorAll("[data-pdf]").forEach(function (button) { button.onclick = function () { action(view.endpoint, button.dataset.pdf, "export-pdf", "POST", reload); }; });
            body.querySelectorAll("[data-read]").forEach(function (button) { button.onclick = function () { action("/Notifications", button.dataset.read, "read", "PUT", reload); }; });
            body.querySelectorAll("[data-delete-notification]").forEach(function (button) { button.onclick = function () { deleteNotification(button.dataset.deleteNotification, reload); }; });
            body.querySelectorAll("[data-restore-entity]").forEach(function (button) { button.onclick = function () { restoreDeleted(button.dataset.restoreEntity, button.dataset.restoreId, reload); }; });
            body.querySelectorAll("[data-pay]").forEach(function (button) { button.onclick = function () { pay(button.dataset.pay); }; });
            document.getElementById("previousPage")?.addEventListener("click", function () { changePage(page - 1); });
            document.getElementById("nextPage")?.addEventListener("click", function () { changePage(page + 1); });
        } catch (error) { body.innerHTML = '<tr><td colspan="' + (view.columns.length + 1) + '" class="table-empty table-error">' + esc(error.message) + "</td></tr>"; meta.textContent = "Không thể tải dữ liệu."; }
    }
    function formControl(item, record) {
        var raw = record && record[item[0]] !== undefined ? record[item[0]] : "";
        var optional = ["userId", "enrollmentId", "courseId", "pdfFilePath", "address", "comment"].includes(item[0]);
        if (item[2] === "date" && raw) raw = new Date(raw).toISOString().slice(0, 10);
        if (item[2] === "datetime-local" && raw) raw = new Date(raw).toISOString().slice(0, 16);
        if (item[2] === "textarea") return '<label>' + item[1] + '<textarea name="' + item[0] + '">' + esc(raw) + "</textarea></label>";
        if (item[2] === "select") return '<label>' + item[1] + '<select name="' + item[0] + '">' + item[3].split(",").map(function (option) { return '<option value="' + option + '"' + (raw === option ? " selected" : "") + ">" + option + "</option>"; }).join("") + "</select></label>";
        if (item[2] === "lookup") return '<label>' + item[1] + '<select name="' + item[0] + '" data-lookup="' + item[3] + '" data-value="' + esc(raw) + '"' + (optional ? "" : " required") + '><option value="">Đang tải lựa chọn…</option></select></label>';
        return '<label>' + item[1] + '<input name="' + item[0] + '" type="' + item[2] + '" value="' + esc(raw) + '"' + (optional || (item[0] === "password" && record) ? "" : " required") + "></label>";
    }
    var lookupSources = {
        students: ["/Students?page=1&pageSize=100", function (row) { return row.fullName + " — " + (row.email || "") + " (#" + row.id + ")"; }],
        teachers: ["/Teachers?page=1&pageSize=100", function (row) { return row.fullName + " — " + (row.specialization || "") + " (#" + row.id + ")"; }],
        courses: ["/Courses?page=1&pageSize=100", function (row) { return row.courseCode + " — " + row.courseName; }],
        exams: ["/Exams?page=1&pageSize=100", function (row) { return row.examName + " — " + row.examType + " (#" + row.id + ")"; }],
        enrollments: ["/Enrollments?page=1&pageSize=100", function (row) { return (row.studentName || "Học viên") + " — " + (row.courseName || "Khóa học") + " (#" + row.id + ")"; }],
        studentUsers: ["/Users?page=1&pageSize=100", function (row) { return row.userName + " — " + row.email + " (#" + row.id + ")"; }, "Student"],
        teacherUsers: ["/Users?page=1&pageSize=100", function (row) { return row.userName + " — " + row.email + " (#" + row.id + ")"; }, "Teacher"]
    };
    async function populateLookups(modal) {
        var selects = modal.querySelectorAll("[data-lookup]");
        await Promise.all(Array.from(selects).map(async function (select) {
            var source = lookupSources[select.dataset.lookup];
            if (!source) return;
            try {
                var response = await apiGet(source[0]);
                var rows = Array.isArray(response) ? response : (response.data || []);
                if (source[2]) rows = rows.filter(function (row) { return row.role === source[2]; });
                var selected = select.dataset.value || "";
                select.innerHTML = '<option value="">-- Chọn ' + select.closest("label").childNodes[0].textContent.trim() + ' --</option>' + rows.map(function (row) { return '<option value="' + row.id + '"' + (String(row.id) === String(selected) ? " selected" : "") + ">" + esc(source[1](row)) + "</option>"; }).join("");
            } catch (_) { select.innerHTML = '<option value="">Không tải được dữ liệu</option>'; }
        }));
    }
    function openForm(name, record, reload) {
        var view = views[name], modal = document.getElementById("adminModal");
        modal.innerHTML = '<div class="modal-backdrop"><form class="modal-card" id="recordForm"><div class="modal-heading"><div><h3>' + (record ? "Cập nhật " : "Thêm ") + view.title.toLowerCase() + '</h3><p>Nhập thông tin và lưu thay đổi.</p></div><button type="button" class="modal-close" id="closeModal">×</button></div><div class="form-grid">' + view.fields.map(function (item) { return formControl(item, record); }).join("") + '</div><div class="modal-actions"><button type="button" class="secondary-button" id="cancelModal">Hủy</button><button class="primary-button" type="submit">Lưu</button></div></form></div>';
        populateLookups(modal);
        function close() { modal.innerHTML = ""; }
        document.getElementById("closeModal").onclick = close; document.getElementById("cancelModal").onclick = close;
        document.getElementById("recordForm").onsubmit = async function (event) {
            event.preventDefault();
            var payload = Object.fromEntries(new FormData(event.target).entries());
            view.fields.forEach(function (item) { if (item[2] === "number" || item[2] === "lookup") payload[item[0]] = payload[item[0]] === "" ? null : Number(payload[item[0]]); });
            if (name === "users" && record && !payload.password) delete payload.password;
            try { await apiRequest(view.endpoint + (record ? "/" + record.id : ""), { method: record ? "PUT" : "POST", body: JSON.stringify(payload) }); close(); alert(record ? "Cập nhật thành công." : "Thêm mới thành công."); reload(); }
            catch (error) { alert(error.message); }
        };
    }
    async function remove(view, id, reload) { if (!(await appConfirm("Bạn có chắc muốn xóa bản ghi này?", "Xóa dữ liệu"))) return; try { await apiDelete(view.endpoint + "/" + id); reload(); } catch (error) { alert(error.message); } }
    async function action(endpoint, id, operation, method, reload) { if (operation === "cancel" && !(await appConfirm("Bạn có chắc muốn hủy?", "Hủy thao tác"))) return; try { await apiRequest(endpoint + "/" + id + "/" + operation, { method: method }); alert("Thao tác thành công."); reload(); } catch (error) { alert(error.message); } }
    async function readAllNotifications(reload) { try { await apiPut("/Notifications/read-all", {}); reload(); } catch (error) { alert(error.message); } }
    async function deleteNotification(id, reload) { if (!(await appConfirm("Bạn có chắc muốn xóa thông báo này?", "Xóa thông báo"))) return; try { await apiDelete("/Notifications/" + id); reload(); } catch (error) { alert(error.message); } }
    async function restoreDeleted(entity, id, reload) { if (!(await appConfirm("Khôi phục bản ghi này?", "Khôi phục dữ liệu"))) return; try { await apiPut("/Trash/" + encodeURIComponent(entity) + "/" + id + "/restore", {}); reload(); } catch (error) { alert(error.message); } }
    async function pay(id) { try { preserveAuthForPaymentReturn(); var result = await apiPost("/Payments/create-vnpay/" + id, {}); var url = result && (result.paymentUrl || result.url); if (url) window.open(url, "_blank", "noopener"); else alert("Đã tạo yêu cầu thanh toán."); } catch (error) { alert(error.message); } }
    async function importStudents(file, reload) { if (!file) return; var form = new FormData(); form.append("file", file); try { await apiRequest("/Students/import-excel", { method: "POST", body: form }); alert("File đã được đưa vào hàng đợi nhập."); reload(); } catch (error) { alert(error.message); } }
    function render(name) {
        var safe = views[name] ? name : "dashboard"; activate(safe); closeSidebar();
        if (safe === "dashboard") { content.innerHTML = dashboardMarkup; if (headerTitle) headerTitle.textContent = "Dashboard"; if (headerDescription) headerDescription.textContent = "Tổng quan hệ thống English Center"; loadUserInformation(); initRefresh(); loadDashboard(); }
        else renderList(safe);
    }
    document.addEventListener("click", function (event) { var link = event.target.closest("[data-route]"); if (!link) return; event.preventDefault(); if (route() === link.dataset.route) render(route()); else location.hash = link.dataset.route; });
    window.addEventListener("hashchange", function () { render(route()); });
    render(route());
})();
