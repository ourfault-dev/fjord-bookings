// The "Keep me posted" form in every page's footer. It is handled here, in the page, with no request to the server.
const form = document.getElementById('newsletter');

form.addEventListener('submit', (event) => {
  event.preventDefault();
  const email = form.elements.email.value.trim();
  const topic = form.dataset.topic || 'fjord news';
  const status = document.getElementById('newsletter-status');
  status.textContent = `Thank you. ${email} will hear about ${topic}.`;
  form.reset();
});

for (const choice of form.querySelectorAll('[data-topic-choice]')) {
  choice.addEventListener('click', () => {
    form.dataset.topic = choice.dataset.topicChoice;
    for (const other of form.querySelectorAll('[data-topic-choice]')) {
      other.setAttribute('aria-pressed', String(other === choice));
    }
  });
}
