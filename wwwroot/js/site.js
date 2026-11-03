/* VertexLearn — shared client-side scripts (loaded on every page) */
document.addEventListener("DOMContentLoaded", function () {

  /* 1. Mobile navigation toggle */
  var toggle = document.getElementById("navToggle");
  var nav = document.getElementById("siteNav");
  if (toggle && nav) {
    toggle.addEventListener("click", function () {
      var isOpen = nav.classList.toggle("open");
      toggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
      toggle.setAttribute("aria-label", isOpen ? "Close menu" : "Open menu");
    });
  }

  /* 2. Confirmation before destructive actions: <form data-confirm="..."> */
  document.querySelectorAll("form[data-confirm]").forEach(function (form) {
    form.addEventListener("submit", function (e) {
      if (!window.confirm(form.getAttribute("data-confirm"))) {
        e.preventDefault();
      }
    });
  });

  /* 3. Show / hide password */
  document.querySelectorAll(".toggle-password").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var input = document.getElementById(btn.getAttribute("data-target"));
      if (!input) return;
      var show = input.type === "password";
      input.type = show ? "text" : "password";
      btn.textContent = show ? "Hide" : "Show";
      btn.setAttribute("aria-label", show ? "Hide password" : "Show password");
    });
  });

  /* 4. Password strength meter (registration) */
  var pw = document.getElementById("Password");
  var meter = document.getElementById("pwStrength");
  var meterText = document.getElementById("pwStrengthText");
  if (pw && meter) {
    pw.addEventListener("input", function () {
      var v = pw.value, score = 0;
      if (v.length >= 8) score++;
      if (/[A-Z]/.test(v) && /[a-z]/.test(v)) score++;
      if (/\d/.test(v)) score++;
      if (/[^A-Za-z0-9]/.test(v) || v.length >= 12) score++;
      meter.value = score;
      if (meterText) {
        meterText.textContent = v.length === 0 ? "" : ["Very weak.", "Weak.", "Fair.", "Good.", "Strong."][score];
      }
    });
  }

  /* 5. Character counters for textareas: <textarea data-counter="500"> */
  document.querySelectorAll("textarea[data-counter]").forEach(function (area) {
    var max = parseInt(area.getAttribute("data-counter"), 10);
    var counter = document.createElement("small");
    counter.className = "char-count";
    counter.setAttribute("aria-live", "polite");
    area.insertAdjacentElement("afterend", counter);
    var update = function () {
      var len = area.value.length;
      counter.textContent = len + " / " + max;
      counter.classList.toggle("over", len > max);
    };
    area.addEventListener("input", update);
    update();
  });

  /* 6. Live cover-image preview (admin course form) */
  document.querySelectorAll("input[data-preview]").forEach(function (input) {
    var img = document.getElementById(input.getAttribute("data-preview"));
    if (!img) return;
    input.addEventListener("change", function () {
      img.src = input.value.trim() || "/images/courses/default.svg";
    });
  });

  /* 7. Range slider output (quiz pass mark) */
  var range = document.getElementById("passRange");
  var out = document.getElementById("passOut");
  if (range && out) {
    range.addEventListener("input", function () { out.textContent = range.value + "%"; });
  }
});
