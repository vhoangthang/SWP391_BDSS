// Chức năng search/filter cho danh sách đăng ký hiến máu
function initDonationListFilter() {
  console.log("initDonationListFilter run");
  const searchInput = document.getElementById("searchInput");
  const statusFilter = document.getElementById("statusFilter");
  const tableBody = document.getElementById("donationTableBody");
  console.log({ searchInput, statusFilter, tableBody });
  if (!searchInput || !statusFilter || !tableBody) return;

  function normalize(str) {
    return (str || "")
      .toLowerCase()
      .normalize("NFD")
      .replace(/\p{Diacritic}/gu, "");
  }
  function filterTable() {
    const keyword = normalize(searchInput.value.trim());
    const status = statusFilter.value;
    Array.from(tableBody.rows).forEach((row) => {
      const id = row.querySelector(".id-col")?.textContent.trim() || "";
      const name = row.querySelector(".name-col")?.textContent.trim() || "";
      const phone = row.querySelector(".bdss-phone")?.textContent.trim() || "";
      const statusVal =
        row.querySelector(".status-col")?.getAttribute("data-status") || "";
      let match = true;
      if (keyword) {
        match =
          normalize(id).includes(keyword) ||
          normalize(name).includes(keyword) ||
          normalize(phone).includes(keyword);
      }
      if (match && status) {
        match = statusVal === status;
      }
      row.style.display = match ? "" : "none";
    });
  }
  searchInput.removeEventListener("input", filterTable);
  statusFilter.removeEventListener("change", filterTable);
  searchInput.addEventListener("input", filterTable);
  statusFilter.addEventListener("change", filterTable);
}

document.addEventListener("DOMContentLoaded", function () {
  // Gọi ngay, nếu chưa đủ phần tử thì thử lại sau 100ms
  if (
    !document.getElementById("searchInput") ||
    !document.getElementById("statusFilter") ||
    !document.getElementById("donationTableBody")
  ) {
    setTimeout(initDonationListFilter, 100);
  } else {
    initDonationListFilter();
  }
});
document.addEventListener("ajaxContentLoaded", initDonationListFilter);
