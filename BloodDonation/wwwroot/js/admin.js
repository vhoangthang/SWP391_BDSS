// Search functionality for User Management page
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const roleFilter = document.getElementById('roleFilter');

    if (searchInput && roleFilter) {
        searchInput.addEventListener('keyup', filterUserTable);
        roleFilter.addEventListener('change', filterUserTable);
    }
});

function filterUserTable() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const roleFilter = document.getElementById('roleFilter').value.toLowerCase();
    const table = document.getElementById('userTable');
    const rows = table.getElementsByTagName('tbody')[0].getElementsByTagName('tr');

    for (let row of rows) {
        const username = row.cells[1].textContent.toLowerCase();
        const role = row.cells[3].textContent.toLowerCase();
        
        const matchesSearch = username.includes(searchTerm);
        const matchesRole = roleFilter === '' || role.includes(roleFilter);
        
        row.style.display = matchesSearch && matchesRole ? '' : 'none';
    }
}

function editUser(userId) {
    // TODO: Implement edit user functionality
    alert('Chức năng chỉnh sửa người dùng sẽ được phát triển sau');
}

function deleteUser(userId) {
    if (confirm('Bạn có chắc chắn muốn xóa người dùng này?')) {
        // TODO: Implement delete user functionality
        alert('Chức năng xóa người dùng sẽ được phát triển sau');
    }
}

// Search functionality for Blood Request Management page
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const statusFilter = document.getElementById('statusFilter');
    const bloodTypeFilter = document.getElementById('bloodTypeFilter');

    if (searchInput && statusFilter && bloodTypeFilter) {
        searchInput.addEventListener('keyup', filterRequestTable);
        statusFilter.addEventListener('change', filterRequestTable);
        bloodTypeFilter.addEventListener('change', filterRequestTable);
    }
});

function filterRequestTable() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const statusFilter = document.getElementById('statusFilter').value.toLowerCase();
    const bloodTypeFilter = document.getElementById('bloodTypeFilter').value;
    const table = document.getElementById('requestTable');
    const rows = table.getElementsByTagName('tbody')[0].getElementsByTagName('tr');

    for (let row of rows) {
        const center = row.cells[1].textContent.toLowerCase();
        const bloodType = row.cells[2].textContent;
        const status = row.cells[6].textContent.toLowerCase();
        
        const matchesSearch = center.includes(searchTerm);
        const matchesStatus = statusFilter === '' || status.includes(statusFilter);
        const matchesBloodType = bloodTypeFilter === '' || bloodType.includes(bloodTypeFilter);
        
        row.style.display = matchesSearch && matchesStatus && matchesBloodType ? '' : 'none';
    }
}

function updateStatus(requestId, status) {
    fetch('/Admin/UpdateBloodRequestStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `requestId=${requestId}&status=${status}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Reload page to show updated statistics
            location.reload();
        } else {
            alert('Có lỗi xảy ra: ' + data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('Có lỗi xảy ra khi cập nhật trạng thái');
    });
}

// Reason modal functionality
document.addEventListener('DOMContentLoaded', function() {
    const reasonModal = document.getElementById('reasonModal');
    if (reasonModal) {
        reasonModal.addEventListener('show.bs.modal', function(event) {
            const button = event.relatedTarget;
            const reason = button.getAttribute('data-reason');
            const reasonText = document.getElementById('reasonText');
            if (reasonText) {
                reasonText.textContent = reason;
            }
        });
    }
}); 