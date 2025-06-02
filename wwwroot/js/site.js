// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    function loadNotifications() {
        $.get('/Notification/GetAll', function (data) {
            // data là HTML hoặc JSON, tùy backend trả về
            $('.notification-list').html(data.html); // Đảm bảo selector đúng
            if (data.count > 0) {
                $('#notificationCount').text(data.count).show();
            } else {
                $('#notificationCount').hide();
            }
        });
    }

    // Gọi khi mở dropdown hoặc định kỳ
    $('#notificationDropdown').on('show.bs.dropdown', function () {
        loadNotifications();
    });

    // Có thể gọi loadNotifications() định kỳ nếu muốn realtime
    // setInterval(loadNotifications, 60000);
});
