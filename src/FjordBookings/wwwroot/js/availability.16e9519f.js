// The "Check availability" button on the landing page, shown with ?variant=js2.
const freeSeats = {
  GEIRANGER: 8,
  LYSE: 5,
};

function seatsLeft(trip) {
  return freeSeats[trip].toString();
}

const button = document.getElementById('check-availability');
button.addEventListener('click', () => {
  const left = seatsLeft(button.dataset.trip);
  document.getElementById('availability-result').textContent = `${left} seats left.`;
});
