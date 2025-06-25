// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
  function bindAjaxLinks() {
    const links = document.querySelectorAll(".sidebar a:not(.logout)");

    links.forEach((link) => {
      link.addEventListener("click", function (e) {
        e.preventDefault();

        fetch(this.href)
          .then((response) => response.text())
          .then((html) => {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");
            const newContent = doc.querySelector(".main").innerHTML;
            document.querySelector(".main").innerHTML = newContent;
            history.pushState({}, "", this.href);

            // Cập nhật active link
            links.forEach((l) => l.classList.remove("active"));
            this.classList.add("active");
          })
          .catch((error) => console.error("Không thể tải nội dung:", error));
      });
    });
  }

  bindAjaxLinks();

  // Khi nhấn nút back/forward trình duyệt
  window.addEventListener("popstate", function () {
    fetch(location.href)
      .then((response) => response.text())
      .then((html) => {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, "text/html");
        const newContent = doc.querySelector(".main").innerHTML;
        document.querySelector(".main").innerHTML = newContent;
        bindAjaxLinks(); // Gán lại sự kiện
      });
  });
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
