var mjml = require('./lib/index')
const inputMJML = `<mjml>
  <mj-head>
    <mj-attributes>
      <mj-all font-size="36px" />
      <mj-all color="blue" />
      <mj-all font-size="40px" />
    </mj-attributes>
  </mj-head>
  <mj-body>
    <mj-container background-color="#d6dde5">
      <mj-section background-color="white">
        <mj-column>
          <mj-text>Hello Boyz</mj-text>
        </mj-column>
      </mj-section>
    </mj-container>
  </mj-body>
</mjml>`

try {
  const { html, errors } = mjml.mjml2html(inputMJML, { beautify: true, level: "soft" })

  if (errors) {
    console.log(errors.map(e => e.formattedMessage).join('\n'))
  }

  console.log(html)
} catch(e) {
  if (e.getMessages) {
  console.log(e.getMessages())
  } else {
    throw e
  }
}
