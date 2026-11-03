/* VertexLearn — quiz page: live progress and "answer every question" check */
document.addEventListener("DOMContentLoaded", function () {
  var form = document.getElementById("quizForm");
  if (!form) return;

  var questions = Array.prototype.slice.call(form.querySelectorAll("fieldset.question"));
  var progress = document.getElementById("quizProgress");
  var countLabel = document.getElementById("answeredCount");
  var errorBox = document.getElementById("quizClientError");
  var submitted = false;

  function unanswered() {
    return questions.filter(function (q) { return !q.querySelector("input[type=radio]:checked"); });
  }

  function refresh() {
    var answered = questions.length - unanswered().length;
    progress.value = answered;
    countLabel.textContent = answered;
    questions.forEach(function (q) {
      if (q.querySelector("input:checked")) q.classList.remove("missing");
    });
  }

  form.addEventListener("change", refresh);

  form.addEventListener("submit", function (e) {
    var missing = unanswered();
    if (missing.length > 0) {
      e.preventDefault();
      missing.forEach(function (q) { q.classList.add("missing"); });
      errorBox.textContent = "Answer all questions before submitting (" + missing.length + " left).";
      errorBox.hidden = false;
      missing[0].scrollIntoView({ block: "center" });
      missing[0].querySelector("input").focus({ preventScroll: true });
      return;
    }
    submitted = true;
  });

  // Warn before leaving a half-finished quiz
  window.addEventListener("beforeunload", function (e) {
    var answered = questions.length - unanswered().length;
    if (!submitted && answered > 0) {
      e.preventDefault();
      e.returnValue = "";
    }
  });

  refresh();
});
