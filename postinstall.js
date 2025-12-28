// postinstall.js
// Creates required folders under ../Assets if they do not exist

const fs = require('fs');
const path = require('path');
const readline = require('readline');

const assetsDir = path.resolve(__dirname, '../');
const examplesDir = path.resolve(__dirname, 'Examples');
const folders = [
  'Editor',
  'Resources',
  'InternalMaps',
  'ExternalMaps',
  'Scripts'
];

// Create folders if they do not exist
folders.forEach(folder => {
  const fullPath = path.join(assetsDir, folder);
  if (!fs.existsSync(fullPath)) {
    fs.mkdirSync(fullPath, { recursive: true });
    console.log(`Created folder: ${fullPath}`);
  } else {
    console.log(`Folder already exists: ${fullPath}`);
  }
});

// Helper to copy files with overwrite prompt
function copyFileWithPrompt(src, dest, rl, cb) {
  if (fs.existsSync(dest)) {
    rl.question(`File ${dest} exists. Overwrite? (y/N): `, answer => {
      if (answer.trim().toLowerCase() === 'y') {
        fs.copyFileSync(src, dest);
        console.log(`Overwritten: ${dest}`);
      } else {
        console.log(`Skipped: ${dest}`);
      }
      cb();
    });
  } else {
    fs.copyFileSync(src, dest);
    console.log(`Copied: ${dest}`);
    cb();
  }
}

// Recursively copy template files from Examples to Assets with user confirmation
function copyTemplates() {
  if (!fs.existsSync(examplesDir)) {
    console.log('No Examples directory found. Skipping template copy.');
    return;
  }

  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  rl.question('Do you want to copy template files from MapLoaderFramework/Examples to your Assets folders? (y/N): ', answer => {
    if (answer.trim().toLowerCase() !== 'y') {
      console.log('Template copy skipped.');
      rl.close();
      return;
    }

    // Gather all files in Examples (recursively)
    function walkDir(dir, relBase = '') {
      let results = [];
      fs.readdirSync(dir, { withFileTypes: true }).forEach(entry => {
        const relPath = path.join(relBase, entry.name);
        const absPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
          results = results.concat(walkDir(absPath, relPath));
        } else {
          results.push({ relPath, absPath });
        }
      });
      return results;
    }

    const files = walkDir(examplesDir);
    let idx = 0;

    function next() {
      if (idx >= files.length) {
        rl.close();
        return;
      }
      const { relPath, absPath } = files[idx++];
      // Skip README.md files
      if (path.basename(relPath).toLowerCase() === 'readme.md') {
        console.log(`Skipped README.md: ${relPath}`);
        next();
        return;
      }
      const destPath = path.join(assetsDir, relPath);
      const destDir = path.dirname(destPath);
      if (!fs.existsSync(destDir)) {
        fs.mkdirSync(destDir, { recursive: true });
      }
      copyFileWithPrompt(absPath, destPath, rl, next);
    }
    next();
  });
}

copyTemplates();
