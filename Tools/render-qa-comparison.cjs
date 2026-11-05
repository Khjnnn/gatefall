// Render the local comparison report; original screenshots are not edited.
const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const { chromium } = require(process.argv[2] || 'playwright');
(async () => {
  const root = path.resolve(__dirname, '..');
  const browser = await chromium.launch({ headless: true, executablePath: process.argv[3] || 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe' });
  try {
    const page = await browser.newPage({ viewport: { width: 1800, height: 1000 }, deviceScaleFactor: 1 });
    await page.goto(pathToFileURL(path.join(root, 'Docs/QA/v050-comparison.html')).href);
    await page.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(img => img.decode())); });
    const invalid = await page.locator('img').evaluateAll(imgs => imgs.filter(i => !i.naturalWidth).map(i => i.src));
    if (invalid.length) throw new Error('Missing comparison screenshots: ' + invalid.join(', '));
    const output = path.join(root, 'Docs/QA/v050-comparison.png');
    await page.screenshot({ path: output, fullPage: true });
    console.log(output + ' (' + fs.statSync(output).size + ' bytes)');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
