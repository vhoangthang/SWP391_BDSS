function initBloodRequestListFilter() {
  const searchInput = document.getElementById("searchInput");
  const statusFilter = document.getElementById("statusFilter");
  const tableBody = document.getElementById("bloodRequestTableBody");
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
      const name = row.querySelector(".name-col")?.textContent.trim() || "";
      const blood = row.querySelector(".blood-col")?.textContent.trim() || "";
      const statusVal =
        row.querySelector(".status-col")?.getAttribute("data-status") || "";
      let match = true;
      if (keyword) {
        match =
          normalize(name).includes(keyword) ||
          normalize(blood).includes(keyword);
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
  if (
    !document.getElementById("searchInput") ||
    !document.getElementById("statusFilter") ||
    !document.getElementById("bloodRequestTableBody")
  ) {
    setTimeout(initBloodRequestListFilter, 100);
  } else {
    initBloodRequestListFilter();
  }
});
document.addEventListener("ajaxContentLoaded", initBloodRequestListFilter);
