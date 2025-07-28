document.getElementById('donationForm').addEventListener('submit', function (e) {
    e.preventDefault();
    const formData = {};
    const form = e.target;

    // Get all selected radio inputs
    const checkedInputs = form.querySelectorAll('input[type="radio"]:checked');
    checkedInputs.forEach(input => {
        formData[input.name] = input.value;
    });

    // Get all selected checkboxes
    const checkedCheckboxes = form.querySelectorAll('input[type="checkbox"]:checked');
    checkedCheckboxes.forEach(input => {
        formData[input.name] = input.value;
    });

    // Check cases where the donor is not eligible to donate blood
    if (
        formData["3_TruocDayAnhChiCoMacCacBenhLietKeKhong"] === "true" ||
        formData["4_KhoiBenhSauMacCacBenh12Thang"] === "true" ||
        formData["4_DuocTruyenMauHoacGayGhepMo"] === "true" ||
        formData["4_TiemVaccine"] === "true" ||
        formData["5_KhoiBenhSauMacCacBenh6Thang"] === "true"
    ) {
        alert("Bạn không đủ điều kiện hiến máu do có tiền sử bệnh lý không phù hợp. Đăng ký sẽ bị huỷ. Bạn sẽ được chuyển về trang chủ.");
        window.location.href = "/";
        return;
    }

    fetch('/DonationForm/SubmitSurvey', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ answers: formData })
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                alert("Đăng ký thành công!");
                window.location.href = '/DonationSummary/Index?appointmentId=' + data.appointmentId;
            } else {
                alert("Lỗi: " + data.message);
                if (data.code === "NO_DONOR") {
                    window.location.href = '/';
                } else if (data.message && data.message.includes("12 tuần")) {
                    window.location.href = '/';
                }
            }
        });
});

document.getElementById('backToHomeBtn').addEventListener('click', function() {
    fetch('/DonationForm/ClearBookingSession', { method: 'POST' })
        .then(() => { window.location.href = '/'; });
});