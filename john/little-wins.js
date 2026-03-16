const wins = [
  {
    title: "Your page looks lovely.",
    text: "And yes, the cute version deserved a sibling."
  },
  {
    title: "You kept it playful.",
    text: "That usually makes simple work feel more alive."
  },
  {
    title: "You asked for fine, not loud.",
    text: "That is exactly why the details get to shine."
  },
  {
    title: "A second page was the right move.",
    text: "It gives the folder a little world instead of a single screen."
  }
];

const winTitle = document.getElementById("winTitle");
const winText = document.getElementById("winText");
const nextWin = document.getElementById("nextWin");

function showNextWin() {
  const currentTitle = winTitle.textContent;
  const options = wins.filter((win) => win.title !== currentTitle);
  const next = options[Math.floor(Math.random() * options.length)];
  winTitle.textContent = next.title;
  winText.textContent = next.text;
}

nextWin.addEventListener("click", showNextWin);
