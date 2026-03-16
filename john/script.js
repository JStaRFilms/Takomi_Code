const lines = [
  "Today feels like a good day to make something neat, simple, and surprisingly calm.",
  "A clean page with a little personality always lands better than a rushed one.",
  "Simple does not mean plain. It means every detail has a reason to stay.",
  "Fine work is usually quiet: balanced spacing, strong contrast, and one clear idea."
];

const message = document.getElementById("message");
const shuffleButton = document.getElementById("shuffleButton");
const clock = document.getElementById("clock");

function chooseNewLine() {
  const current = message.textContent;
  const options = lines.filter((line) => line !== current);
  const nextLine = options[Math.floor(Math.random() * options.length)];
  message.textContent = nextLine;
}

function updateClock() {
  const now = new Date();
  clock.textContent = now.toLocaleTimeString();
}

shuffleButton.addEventListener("click", chooseNewLine);
updateClock();
window.setInterval(updateClock, 1000);
