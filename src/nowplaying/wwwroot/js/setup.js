// Define import map for ES modules
const importMap = {
  imports: {
    'vue': 'https://unpkg.com/vue@3.4.21/dist/vue.esm-browser.prod.js'
  }
};
const script = document.createElement('script');
script.type = 'importmap';
script.textContent = JSON.stringify(importMap);
document.head.appendChild(script);
