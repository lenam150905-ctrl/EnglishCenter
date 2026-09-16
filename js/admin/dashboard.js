// =========================
// BẢO VỆ ADMIN
// =========================

if (requireRole("Admin")) {
    // Shell is initialized once. Content is rendered by admin-router.js.
    loadUserInformation();
    initSidebar();
    initLogout();
}


// =========================
// KHỞI TẠO
// =========================

function initDashboard() {

    loadUserInformation();

    initSidebar();

    initLogout();

    initRefresh();

    loadDashboard();
}


// =========================
// USER INFORMATION
// =========================

function loadUserInformation() {

    const userName =
        getCurrentUserName() || "Admin";

    const role =
        getCurrentRole() || "Admin";


    setText(
        "sidebarUserName",
        userName
    );

    setText(
        "sidebarUserRole",
        role
    );

    setText(
        "headerUserName",
        userName
    );

    setText(
        "headerRole",
        role
    );

    setText(
        "welcomeUserName",
        userName
    );

    setText(
        "systemUserName",
        userName
    );

    setText(
        "systemRole",
        role
    );


    const firstLetter =
        userName
            .trim()
            .charAt(0)
            .toUpperCase() || "A";


    setText(
        "sidebarAvatar",
        firstLetter
    );

    setText(
        "headerAvatar",
        firstLetter
    );
}


// =========================
// LOAD DASHBOARD API
// =========================

async function loadDashboard() {

    setStatisticsLoading();


    try {

        const data =
            await apiGet(
                "/Report/dashboard"
            );


        console.log(
            "Dashboard data:",
            data
        );


        setText(
            "studentCount",
            formatNumber(
                data.totalStudents
            )
        );


        setText(
            "courseCount",
            formatNumber(
                data.totalCourses
            )
        );


        setText(
            "enrollmentCount",
            formatNumber(
                data.totalEnrollments
            )
        );


        setText(
            "examCount",
            formatNumber(
                data.totalExams
            )
        );


        setText(
            "gradeCount",
            formatNumber(
                data.totalGrades
            )
        );


        setText(
            "certificateCount",
            formatNumber(
                data.totalCertificates
            )
        );

    } catch (error) {

        console.error(
            "Load Dashboard Error:",
            error
        );


        setStatisticsError();
    }
}


// =========================
// LOADING
// =========================

function setStatisticsLoading() {

    const ids = [
        "studentCount",
        "courseCount",
        "enrollmentCount",
        "examCount",
        "gradeCount",
        "certificateCount"
    ];


    ids.forEach(
        function (id) {

            setText(
                id,
                "..."
            );
        }
    );
}


// =========================
// ERROR
// =========================

function setStatisticsError() {

    const ids = [
        "studentCount",
        "courseCount",
        "enrollmentCount",
        "examCount",
        "gradeCount",
        "certificateCount"
    ];


    ids.forEach(
        function (id) {

            setText(
                id,
                "!"
            );
        }
    );
}


// =========================
// FORMAT NUMBER
// =========================

function formatNumber(value) {

    const number =
        Number(value ?? 0);


    if (
        Number.isNaN(number)
    ) {
        return "0";
    }


    return number
        .toLocaleString(
            "vi-VN"
        );
}


// =========================
// REFRESH
// =========================

function initRefresh() {

    const refreshButton =
        document.getElementById(
            "refreshButton"
        );


    if (!refreshButton) {
        return;
    }


    refreshButton.addEventListener(
        "click",
        async function () {

            refreshButton.disabled =
                true;

            refreshButton.textContent =
                "↻ Đang tải...";


            await loadDashboard();


            refreshButton.disabled =
                false;

            refreshButton.textContent =
                "↻ Làm mới";
        }
    );
}


// =========================
// SIDEBAR
// =========================

function initSidebar() {

    const sidebar =
        document.getElementById(
            "sidebar"
        );

    const menuButton =
        document.getElementById(
            "menuButton"
        );

    const overlay =
        document.getElementById(
            "sidebarOverlay"
        );


    if (
        !sidebar ||
        !menuButton ||
        !overlay
    ) {
        return;
    }


    menuButton.addEventListener(
        "click",
        function () {

            sidebar.classList.toggle(
                "open"
            );

            overlay.classList.toggle(
                "show"
            );
        }
    );


    overlay.addEventListener(
        "click",
        function () {

            closeSidebar();
        }
    );


    function closeSidebar() {

        sidebar.classList.remove(
            "open"
        );

        overlay.classList.remove(
            "show"
        );
    }


    window.addEventListener(
        "resize",
        function () {

            if (
                window.innerWidth > 850
            ) {
                closeSidebar();
            }
        }
    );
}


// =========================
// LOGOUT
// =========================

function initLogout() {

    const logoutButton =
        document.getElementById(
            "logoutButton"
        );


    if (!logoutButton) {
        return;
    }


    logoutButton.addEventListener(
        "click",
        function () { logout(); }
    );
}


// =========================
// HELPER
// =========================

function setText(
    elementId,
    value
) {

    const element =
        document.getElementById(
            elementId
        );


    if (element) {

        element.textContent =
            value ?? "";
    }
}
