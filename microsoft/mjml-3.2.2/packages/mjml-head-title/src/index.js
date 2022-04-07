export default {
  name: "mj-title",
  handler: (el, { $container }) => {
    const innerText = el.children.map(child => (child.type === 'text' || child.nodeType === Node.TEXT_NODE) && child.data).join('')

    $container('title').text(innerText)
  }
}
