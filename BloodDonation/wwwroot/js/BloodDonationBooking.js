// Tạo lịch động và lưu ngày đã chọn vào localStorage
const days = ["CN", "T2", "T3", "T4", "T5", "T6", "T7"];
const datePicker = document.getElementById("datePicker");
const today = new Date();
const numDaysToShow = 15;

function formatDateLocal(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

for (let i = 0; i < numDaysToShow; i++) {
  const date = new Date(today);
  date.setDate(today.getDate() + i);
  const dayOfWeek = days[date.getDay()];
  const day = date.getDate();

  const div = document.createElement("div");
  div.classList.add("date");

  const thisDateStr = formatDateLocal(date);
  const selectedDate = localStorage.getItem("selectedDate");

  if (thisDateStr === selectedDate) {
    div.classList.add("active");
  }

  div.innerHTML = `${dayOfWeek}<br>${day}`;
  div.addEventListener("click", function () {
    document.querySelectorAll(".date").forEach(el => el.classList.remove("active"));
    div.classList.add("active");

    const selectedDate = formatDateLocal(date);
    localStorage.setItem("selectedDate", selectedDate);
  });

  datePicker.appendChild(div);
}

// chuyển màu time slot để dễ nhìn
const timeButtons = document.querySelectorAll('.time-slots button');

  timeButtons.forEach(button => {
    button.addEventListener('click', () => {
      timeButtons.forEach(btn => btn.classList.remove('selected')); // bỏ chọn các nút khác
      button.classList.add('selected'); // đánh dấu nút được chọn
    });
  });
// Lưu nhóm máu
const bloodButtons = document.querySelectorAll('.blood-types button');
bloodButtons.forEach(button => {
  button.addEventListener('click', () => {
    bloodButtons.forEach(b => b.classList.remove('selected'));
    button.classList.add('selected');
    localStorage.setItem('selectedBloodType', button.textContent.trim());
  });
});

// Lưu khung giờ
const timeButtons1 = document.querySelectorAll('.time-slots button');
timeButtons1.forEach(button => {
  button.addEventListener('click', () => {
    timeButtons1.forEach(b => b.classList.remove('selected'));
    button.classList.add('selected');
    localStorage.setItem('selectedTime', button.textContent);
  });
});

