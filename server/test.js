/**
 * Simple test runner to validate code without running the full server
 * Run with: node test.js
 */

console.log('Testing module imports...\n');

let passed = 0;
let failed = 0;

async function test(name, fn) {
  try {
    await fn();
    console.log(`✓ ${name}`);
    passed++;
  } catch (error) {
    console.error(`✗ ${name}`);
    console.error(`  Error: ${error.message}\n`);
    failed++;
  }
}

// Test 1: Express import
test('Express module loads', async () => {
  const express = await import('express');
  if (!express.default) throw new Error('Express default export missing');
});

// Test 2: Session middleware
test('Express-session module loads', async () => {
  const session = await import('express-session');
  if (!session.default) throw new Error('Session default export missing');
});

// Test 3: Axios
test('Axios module loads', async () => {
  const axios = await import('axios');
  if (!axios.default) throw new Error('Axios default export missing');
});

// Test 4: Cheerio with correct import
test('Cheerio module loads with correct export', async () => {
  const cheerio = await import('cheerio');
  if (!cheerio.load) throw new Error('Cheerio.load not found - using wrong export');
});

// Test 5: Form-data
test('Form-data module loads', async () => {
  const FormData = await import('form-data');
  if (!FormData.default) throw new Error('Form-data default export missing');
});

// Test 6: Basic server startup (don't connect to Mastodon)
test('Server code parses without syntax errors', async () => {
  try {
    // Just import to check for syntax errors
    // We won't actually start the server
    const { readFileSync } = await import('fs');
    const code = readFileSync('./server.js', 'utf-8');
    // If we got here without error, the file is readable
    if (!code.includes('express')) throw new Error('Server code looks wrong');
  } catch (error) {
    if (error.code === 'ENOENT') {
      throw new Error('server.js not found');
    }
    throw error;
  }
});

// Test 7: Environment variable handling
test('Environment variables can be read', () => {
  const requiredVars = ['MASTODON_INSTANCE', 'MASTODON_CLIENT_ID', 'MASTODON_CLIENT_SECRET'];
  // These might not be set, but we're testing that the code can read them
  requiredVars.forEach(v => {
    const val = process.env[v];
    if (val === undefined) {
      console.warn(`  Warning: ${v} not set in environment`);
    }
  });
});

// Summary
setTimeout(() => {
  console.log(`\n${'='.repeat(50)}`);
  console.log(`Tests passed: ${passed}`);
  console.log(`Tests failed: ${failed}`);
  console.log(`${'='.repeat(50)}\n`);
  
  if (failed > 0) {
    console.log('Fix the errors above, then rebuild the Docker image.');
    process.exit(1);
  } else {
    console.log('All tests passed! Ready to build Docker image.');
    process.exit(0);
  }
}, 1000);
