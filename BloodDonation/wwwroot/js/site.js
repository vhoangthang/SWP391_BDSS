// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
  function bindAjaxLinks() {
    const links = document.querySelectorAll(".sidebar a:not(.logout)");
    links.forEach((link) => {
      link.addEventListener("click", function (e) {
        e.preventDefault();
        const main = document.querySelector(".main");
        fetch(this.href)
          .then((response) => response.text())
          .then((html) => {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");
            const newContent = doc.querySelector(".main").innerHTML;
            main.innerHTML = newContent;
            history.pushState({}, "", this.href);
            // Update active link
            links.forEach((l) => l.classList.remove("active"));
            this.classList.add("active");
            // Notify that content has loaded so other scripts can re-initialize
            const event = new Event("ajaxContentLoaded");
            document.dispatchEvent(event);
          })
          .catch((error) => {
            console.error("Không thể tải nội dung:", error);
          });
      });
    });
  }

  bindAjaxLinks();

  window.addEventListener("popstate", function () {
    fetch(location.href)
      .then((response) => response.text())
      .then((html) => {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, "text/html");
        const newContent = doc.querySelector(".main").innerHTML;
        document.querySelector(".main").innerHTML = newContent;
        bindAjaxLinks(); // Re-assign event
        // Notify that content has loaded so other scripts can re-initialize
        const event = new Event("ajaxContentLoaded");
        document.dispatchEvent(event);
      });
  });

  // Fix addEventListener on null element (line 67)
  var modal = document.getElementById("healthHistoryModal");
  if (modal) {
    modal.addEventListener("shown.bs.modal", function () {
      console.log("✔ Modal đã mở đúng");
    });
  }

  // When entering the page for the first time, also dispatch ajaxContentLoaded so search/filter scripts always initialize
  const event = new Event("ajaxContentLoaded");
  document.dispatchEvent(event);
});

function confirmComplete() {
  return confirm("Bạn có chắc chắn muốn đánh dấu là đã hoàn tất hiến máu?");
}

function toggleHealthHistory() {
  const panel = document.getElementById("health-history");
  panel.classList.toggle("show");
}
function toggleSurvey() {
  var table = document.getElementById("surveyTable");
  if (table) {
    table.style.display = table.style.display === "none" ? "block" : "none";
  }
}

const signUpButton = document.getElementById("signUp");
const signInButton = document.getElementById("signIn");
const container = document.getElementById("container");

signUpButton.addEventListener("click", () => {
  container.classList.add("right-panel-active");
});

signInButton.addEventListener("click", () => {
  container.classList.remove("right-panel-active");
});
